﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpUpgradeProject : AbstractUpdateProjectTest
    {
        public CSharpUpgradeProject(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        private void InvokeFix(string version = "latest")
        {
            VisualStudio.Editor.SetText(@$"
#error version:{version}
");
            VisualStudio.Editor.Activate();

            VisualStudio.Editor.PlaceCaret($"version:{version}");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction($"Upgrade this project to C# language version '{version}'", applyFix: true);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/38301"), Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public void CPSProject_GeneralPropertyGroupUpdated()
        {
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.CSharpNetStandardClassLibrary, LanguageNames.CSharp);
            VisualStudio.SolutionExplorer.RestoreNuGetPackages(project);

            InvokeFix();
            VerifyPropertyOutsideConfiguration(GetProjectFileElement(project), "LangVersion", "latest");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public void LegacyProject_AllConfigurationsUpdated()
        {
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            VisualStudio.SolutionExplorer.AddCustomProject(project, ".csproj", $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition=""'$(Configuration)' == ''"">Debug</Configuration>
    <Platform Condition=""'$(Platform)' == ''"">x64</Platform>
    <ProjectGuid>{{F4233BA4-A4CB-498B-BBC1-65A42206B1BA}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>{ProjectName}</RootNamespace>
    <AssemblyName>{ProjectName}</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
    <LangVersion>7.0</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x86'"">
    <OutputPath>bin\x86\Debug\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\x86\Release\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x64'"">
    <OutputPath>bin\x64\Debug\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\x64\Release\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>");
            VisualStudio.SolutionExplorer.AddFile(project, "C.cs", open: true);

            InvokeFix(version: "7.3");
            VerifyPropertyInEachConfiguration(GetProjectFileElement(project), "LangVersion", "7.3");
        }

        [WorkItem(23342, "https://github.com/dotnet/roslyn/issues/23342")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUpgradeProject)]
        public void LegacyProject_MultiplePlatforms_AllConfigurationsUpdated()
        {
            var project = new ProjectUtils.Project(ProjectName);

            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            VisualStudio.SolutionExplorer.AddCustomProject(project, ".csproj", $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition=""'$(Configuration)' == ''"">Debug</Configuration>
    <Platform Condition=""'$(Platform)' == ''"">x64</Platform>
    <ProjectGuid>{{F4233BA4-A4CB-498B-BBC1-65A42206B1BA}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>{ProjectName}</RootNamespace>
    <AssemblyName>{ProjectName}</AssemblyName>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x86'"">
    <OutputPath>bin\x86\Debug\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>7.2</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\x86\Release\</OutputPath>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Debug|x64'"">
    <OutputPath>bin\x64\Debug\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>7.0</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\x64\Release\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>7.1</LangVersion>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>");

            VisualStudio.SolutionExplorer.AddFile(project, "C.cs", open: true);

            InvokeFix(version: "7.3");
            VerifyPropertyInEachConfiguration(GetProjectFileElement(project), "LangVersion", "7.3");
        }
    }
}
