﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated backing field for an automatically/semi-automatically implemented property.
    /// </summary>
    internal sealed class SynthesizedBackingFieldSymbol : FieldSymbolWithAttributesAndModifiers
    {
        [Flags]
        private enum Flags
        {
            HasInitializer = 1 << 0,
            IsCreatedForFieldKeyword = 1 << 1,
            IsEarlyConstructed = 1 << 2,
        }

        private readonly SourcePropertySymbolBase _property;
        private readonly string _name;
        private readonly Flags _backingFieldFlags;

        internal bool HasInitializer => (_backingFieldFlags & Flags.HasInitializer) != 0;
        internal bool IsCreatedForFieldKeyword => (_backingFieldFlags & Flags.IsCreatedForFieldKeyword) != 0;
        internal bool IsEarlyConstructed => (_backingFieldFlags & Flags.IsEarlyConstructed) != 0;

        protected override DeclarationModifiers Modifiers { get; }

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbolBase property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer,
            bool isCreatedForFieldKeyword,
            bool isEarlyConstructed)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            _name = name;

            Modifiers = DeclarationModifiers.Private |
                (isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None) |
                (isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None);

            _property = property;
            if (hasInitializer)
            {
                _backingFieldFlags |= Flags.HasInitializer;
            }

            if (isCreatedForFieldKeyword)
            {
                _backingFieldFlags |= Flags.IsCreatedForFieldKeyword;
            }


            if (isEarlyConstructed)
            {
                _backingFieldFlags |= Flags.IsEarlyConstructed;
            }

            // If it's not early constructed, it must have been created for field keyword.
            Debug.Assert(IsEarlyConstructed || IsCreatedForFieldKeyword);
        }

        protected override IAttributeTargetSymbol AttributeOwner
            => _property.AttributesOwner;

        internal override Location ErrorLocation
            => _property.Location;

        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => _property.AttributeDeclarationSyntaxList;

        public override Symbol AssociatedSymbol
            => _property;

        public override ImmutableArray<Location> Locations
            => _property.Locations;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            => _property.TypeWithAnnotations;

        internal override bool HasPointerType
            => _property.HasPointerType;

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);
            Debug.Assert(arguments.Diagnostics is BindingDiagnosticBag);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.FixedBufferAttribute))
            {
                // error CS8362: Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute on property
                ((BindingDiagnosticBag)arguments.Diagnostics).Add(ErrorCode.ERR_DoNotUseFixedBufferAttrOnProperty, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else
            {
                base.DecodeWellKnownAttribute(ref arguments);
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;

            // do not emit CompilerGenerated attributes for fields inside compiler generated types:
            if (!this.ContainingType.IsImplicitlyDeclared)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }

        public override string Name
            => _name;

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            => null;

        public override Symbol ContainingSymbol
            => _property.ContainingSymbol;

        public override NamedTypeSymbol ContainingType
            => _property.ContainingType;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        internal override bool HasRuntimeSpecialName
            => false;

        public override bool IsImplicitlyDeclared
            => true;

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);

            if (!allAttributeSyntaxNodes.IsEmpty && _property.IsAutoPropertyWithGetAccessor)
            {
                CheckForFieldTargetedAttribute(diagnostics);
            }
        }

        private void CheckForFieldTargetedAttribute(BindingDiagnosticBag diagnostics)
        {
            var languageVersion = this.DeclaringCompilation.LanguageVersion;
            if (languageVersion.AllowAttributesOnBackingFields())
            {
                return;
            }

            foreach (var attribute in AttributeDeclarationSyntaxList)
            {
                if (attribute.Target?.GetAttributeLocation() == AttributeLocation.Field)
                {
                    diagnostics.Add(
                        new CSDiagnosticInfo(ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable,
                            languageVersion.ToDisplayString(),
                            new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureAttributesOnBackingFields.RequiredVersion())),
                        attribute.Target.Location);
                }
            }
        }
    }
}
