// =====================================================================
//  This file is based on code from the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Text;
using Microsoft.Xrm.Sdk;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace SendParticleCommand
{
    /// <inheritdoc />
    /// <summary>
    /// Base class for all plug-in classes.
    /// </summary>    
    public abstract class PluginBase : IPlugin
    {
        /// <summary>
        /// Plug-in context object. 
        /// </summary>
        protected class LocalPluginContext
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "LocalPluginContext")]
            internal IServiceProvider ServiceProvider { get; private set; }

            /// <summary>
            /// The Microsoft Dynamics CRM organization service.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "LocalPluginContext")]
            internal IOrganizationService OrganizationService { get; }

            /// <summary>
            /// IPluginExecutionContext contains information that describes the run-time environment in which the plug-in executes, information related to the execution pipeline, and entity business information.
            /// </summary>
            internal IPluginExecutionContext PluginExecutionContext { get; }

            /// <summary>
            /// Synchronous registered plug-ins can post the execution context to the Microsoft Azure Service Bus. <br/> 
            /// It is through this notification service that synchronous plug-ins can send brokered messages to the Microsoft Azure Service Bus.
            /// </summary>
            private IServiceEndpointNotificationService NotificationService { get; }

            /// <summary>
            /// Provides logging run-time trace information for plug-ins. 
            /// </summary>
            internal ITracingService TracingService { get; }

            private LocalPluginContext() { }

            /// <summary>
            /// Helper object that stores the services available in this plug-in.
            /// </summary>
            /// <param name="serviceProvider"></param>
            internal LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                {
                    throw new ArgumentNullException(nameof(serviceProvider));
                }

                // Obtain the execution context service from the service provider.
                PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Get the notification service from the service provider.
                NotificationService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));

                // Obtain the organization factory service from the service provider.
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the organization service.
                OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);
            }

            /// <summary>
            /// Writes a trace message to the CRM trace log.
            /// </summary>
            /// <param name="message">Message name to trace.</param>
            internal void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || TracingService == null)
                {
                    return;
                }

                if (PluginExecutionContext == null)
                {
                    TracingService.Trace(message);
                }
                else
                {
                    TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        PluginExecutionContext.CorrelationId,
                        PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "PluginBase")]
        private string ChildClassName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginBase"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref=" cred="Type"/> of the derived class.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "PluginBase")]
        internal PluginBase(Type childClassName)
        {
            ChildClassName = childClassName.ToString();
        }

        /// <inheritdoc />
        /// <summary>
        /// Main entry point for he business logic that the plug-in is to execute.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "CrmVSSolution411.NewProj.PluginBase+LocalPluginContext.Trace(System.String)", Justification = "Execute")]
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Construct the local plug-in context.
            LocalPluginContext localContext = new LocalPluginContext(serviceProvider);

            localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));

#if DEBUG
            TraceParameters(true, localContext);
#endif

            try
            {
                // Invoke the custom implementation 
                ExecuteCrmPlugin(localContext);
                // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                // guard against multiple executions.

#if DEBUG
                TraceParameters(false, localContext);
#endif
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            }
            finally
            {
                localContext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }

        /// <summary>
        /// Placeholder for a custom plug-in implementation. 
        /// </summary>
        /// <param name="localcontext">Context for the current plug-in.</param>
        protected virtual void ExecuteCrmPlugin(LocalPluginContext localcontext)
        {
            // Do nothing. 
        }

        private static void TraceParameters(bool input, LocalPluginContext localContext)
        {
            ParameterCollection parameters = input
                ? localContext.PluginExecutionContext.InputParameters
                : localContext.PluginExecutionContext.OutputParameters;

            if (parameters == null || parameters.Count == 0)
                return;

            try
            {
                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    StringBuilder sb = new StringBuilder();

                    var typeFullname =  (object) parameter.Value?.GetType().FullName;
                    string parameterType = input ? "Input" : "Output";
                    string typeFullnameString = typeFullname != null ? typeFullname.ToString() : "NULL";
                    sb.Append($"{parameterType} Parameter({typeFullnameString}): {parameter.Key}: ");

                    switch (typeFullname)
                    {
                        case null:
                            sb.Append("NULL");
                            break;
                        case "System.String":
                        case "System.Decimal":
                        case "System.Double":
                        case "System.Int32":
                        case "System.Boolean":
                        case "System.Single":
                            sb.Append(parameter.Value);
                            break;
                        case "System.DateTime":
                            sb.Append(((DateTime)parameter.Value).ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz"));
                            break;
                        case "Microsoft.Xrm.Sdk.EntityReference":
                            var entityReference = (EntityReference)parameter.Value;
                            sb.Append($"Id: {entityReference.Id}, LogicalName: {entityReference.LogicalName}");
                            break;
                        case "Microsoft.Xrm.Sdk.Entity":
                            var entity = (Entity)parameter.Value;
                            sb.Append($"Id: {entity.Id}, LogicalName: {entity.LogicalName}");
                            break;
                        case "Microsoft.Xrm.Sdk.EntityCollection":
                            var entityCollection = (EntityCollection)parameter.Value;
                            foreach (var e in entityCollection.Entities)
                            {
                                sb.Append(Environment.NewLine);
                                sb.Append($"Id: {e.Id}, LogicalName: {e.LogicalName}");
                            }
                            break;
                        case "Microsoft.Xrm.Sdk.OptionSetValue":
                            sb.Append(((OptionSetValue)parameter.Value).Value);
                            break;
                        case "Microsoft.Xrm.Sdk.Money":
                            sb.Append(((Money)parameter.Value).Value.ToString(CultureInfo.CurrentCulture));
                            break;
                        default:
                            sb.Append($"Undefined Type - {typeFullname}");
                            break;
                    }

                    localContext.TracingService.Trace(sb.ToString());
                }
            }
            catch (Exception e)
            {
                localContext.TracingService.Trace($"Error tracing parameters: {e.Message}");
            }
        }
    }
}