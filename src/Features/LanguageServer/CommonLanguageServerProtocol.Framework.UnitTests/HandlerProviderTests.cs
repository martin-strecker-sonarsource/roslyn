﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public partial class HandlerProviderTests
{
    private const string _method = "SomeMethod";
    private const string _wrongMethod = "WrongMethod";
    private static readonly Type _requestType = typeof(int);
    private static readonly Type _responseType = typeof(string);
    private static readonly Type _wrongResponseType = typeof(long);

    private static readonly IMethodHandler _expectedMethodHandler = new TestMethodHandler();

    [Fact]
    public void GetMethodHandler_ViaGetRequiredServices_Succeeds()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: true, supportsGetRegisteredServices: false);

        var methodHander = handlerProvider.GetMethodHandler(_method, _requestType, _responseType);

        Assert.Same(_expectedMethodHandler, methodHander);
    }

    [Fact]
    public void GetMethodHandler_ViaGetRegisteredServices_Succeeds()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: false, supportsGetRegisteredServices: true);

        var methodHander = handlerProvider.GetMethodHandler(_method, _requestType, _responseType);

        Assert.Same(_expectedMethodHandler, methodHander);
    }

    [Fact]
    public void GetMethodHandler_WrongMethod_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: true, supportsGetRegisteredServices: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandler(_wrongMethod, _requestType, _responseType));
    }

    [Fact]
    public void GetMethodHandler_WrongResponseType_Throws()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: true, supportsGetRegisteredServices: false);

        Assert.Throws<InvalidOperationException>(() => handlerProvider.GetMethodHandler(_method, _requestType, _wrongResponseType));
    }

    [Fact]
    public void GetRegisteredMethods_GetRequiredServices()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: true, supportsGetRegisteredServices: false);

        var registeredMethods = handlerProvider.GetRegisteredMethods();

        Assert.Collection(registeredMethods,
            (r) => Assert.Equal(_method, r.MethodName));
    }

    [Fact]
    public void GetRegisteredMethods_GetRegisteredServices()
    {
        var handlerProvider = GetHandlerProvider(supportsRequiredServices: false, supportsGetRegisteredServices: true);

        var registeredMethods = handlerProvider.GetRegisteredMethods();

        Assert.Collection(registeredMethods,
            (r) => Assert.Equal(_method, r.MethodName));
    }

    private static HandlerProvider GetHandlerProvider(bool supportsRequiredServices, bool supportsGetRegisteredServices)
    {
        var lspServices = GetLspServices(supportsRequiredServices, supportsGetRegisteredServices);
        var handler = new HandlerProvider(lspServices);

        return handler;
    }

    private static ILspServices GetLspServices(bool supportsRequiredServices, bool supportsGetRegisteredServices)
    {
        var services = new List<(Type, object)> { (typeof(IMethodHandler), _expectedMethodHandler) };
        var lspServices = new TestLspServices(services, supportsRequiredServices, supportsGetRegisteredServices);
        return lspServices;
    }

    [LanguageServerEndpoint(_method)]
    internal class TestMethodHandler : IRequestHandler<int, string, TestRequestContext>
    {
        public bool MutatesSolutionState => true;

        public static string Method = _method;

        public static Type RequestType = typeof(int);

        public static Type ResponseType = typeof(string);

        public object? GetTextDocumentIdentifier(int request)
        {
            return null;
        }

        public Task<string> HandleRequestAsync(int request, TestRequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult("stuff");
        }
    }

    private class TestMethodHandlerWithoutAttribute : INotificationHandler<TestRequestContext>
    {
        public bool MutatesSolutionState => true;

        public Task HandleNotificationAsync(TestRequestContext requestContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
