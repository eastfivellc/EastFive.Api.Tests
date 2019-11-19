﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static EastFive.Api.HttpApplication;

namespace EastFive.Api.Tests
{
    public interface ITestApplication : IInvokeApplication
    {
        IDictionary<string, string> Headers { get; }

        object CastResourceProperty(object value, Type propertyType);

        void SetInstigator(Type type, InstigatorDelegate instigator, bool clear = false);

        void SetInstigatorGeneric(Type type, InstigatorDelegateGeneric instigator, bool clear = true);
    }
}
