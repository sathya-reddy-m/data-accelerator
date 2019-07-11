﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Http;
using DataX.Contract;
using System;
using DataX.Gateway.Contract;
using DataX.ServiceHost;
using DataX.ServiceHost.ServiceFabric;
using System.Collections.Generic;
using System.Fabric;

namespace DataX.Utilities.Web
{
    public static class RolesCheck
    {
        // Default role names for reader/writer. These are made public so that service can override these values.
        public static string ReaderRoleName { get; set; } = "DataXReader";
        public static string WriterRoleName { get; set; } = "DataXWriter";       

        private static readonly HashSet<string> _ClientWhitelist = new HashSet<string>();

        public static void EnsureWriter(HttpRequest request, bool isLocal)
        {
            // If the request is for localhost, ignore roles check. This is to enable local onebox scenario
            if (!isLocal)
            {
                EnsureWriter(request);
            }
        }
        public static void TestHelper()
        {
            if (HostUtil.InServiceFabric)
            {                
                var serviceKeyvaultName = ServiceFabricUtil.GetServiceKeyVaultName().Result.ToString();
                var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
                var serviceEnvironmenConfig = configPackage.Settings.Sections["ServiceEnvironment"];                
                var testClientId = serviceEnvironmenConfig.Parameters["TestClientId"].Value;
                _ClientWhitelist.Add(KeyVault.KeyVault.GetSecretFromKeyvault(serviceKeyvaultName, testClientId));
            }            
        }

        public static void EnsureWriter(HttpRequest request)
        {
            // Ensure* methods only work when auth is handled at the Gateway in Service Fabric setup
            // Otherwise ASP.NET Core is used and does not require this check
            if(!HostUtil.InServiceFabric)
            {
                return;
            }

            var userrole = request.Headers[Constants.UserRolesHeader];
            var userID = request.Headers[Constants.UserIdHeader];
            TestHelper();
            
            Ensure.NotNull(userrole, "userrole");

            if (!userrole.ToString().Contains(WriterRoleName) && !string.IsNullOrEmpty(userID) && !_ClientWhitelist.Contains(userID))
            {
                throw new System.Exception($"{WriterRoleName} role needed to perform this action.  User has the following roles: {userrole}");
            }
        }

        public static void EnsureReader(HttpRequest request, bool isLocal)
        {
            // If the request is for localhost, ignore roles check. This is to enable local onebox scenario
            if (!isLocal)
            {
                EnsureReader(request);
            }
        }

        public static void EnsureReader(HttpRequest request)
        {
            // Ensure* methods only work when auth is handled at the Gateway in Service Fabric setup
            // Otherwise ASP.NET Core is used and does not require this check
            if (!HostUtil.InServiceFabric)
            {
                return;
            }

            var userrole = request.Headers[Constants.UserRolesHeader];
            var userID = request.Headers[Constants.UserIdHeader];
            TestHelper();
            Ensure.NotNull(userrole, "userrole");

            if (!userrole.ToString().Contains(ReaderRoleName) && !userrole.ToString().Contains(WriterRoleName) && !string.IsNullOrEmpty(userID) && !_ClientWhitelist.Contains(userID))
            {
                throw new System.Exception($"{ReaderRoleName} role needed to perform this action.  User has the following roles: {userrole}");
            }
        }
    }
}

