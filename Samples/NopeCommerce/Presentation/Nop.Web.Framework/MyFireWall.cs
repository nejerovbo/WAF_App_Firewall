﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Walter;
using Walter.Web.FireWall;
using Walter.Web.FireWall.Models;

namespace Nop.Web.Framework
{
    /// <summary>
    /// you can inject your own firewall by basing your firewall on the FireWallBased class.
    /// </summary>
    /// <remarks>
    /// When overriding the firewall class you have the possibility to interact with requests and incidents at a very early stage    /// 
    /// </remarks>
    public class MyFireWall : FireWallBase
    {
        ILogger _logger;
        public MyFireWall(IServiceProvider serviceProvider, IMemoryCache memory, ILoggerFactory loggerFactory)
            : base(serviceProvider: serviceProvider, loggerFactory: loggerFactory, memoryCache: memory)
        {
            _logger = loggerFactory?.CreateLogger<MyFireWall>();
            base.OnCaughtExceptiont += MyFireWall_OnCaughtExceptiont;
            base.OnIncident += MyFireWall_OnIncident;
            base.OnPhishyRequest += MyFireWall_OnPhishyRequest;
            base.OnUserTypeChange += MyFireWall_OnUserTypeChange;
            base.OnGuardAction += MyFireWall_OnGuardAction;

            base.Trigger_OnFireWallCreated(this);

        }

        private void MyFireWall_OnGuardAction(object sender, Walter.Web.FireWall.EventArguments.GuardActionEventArgs e)
        {
            _logger.Lazy().LogInformation(eventId: new EventId(Line(), Method())
                                        , message: "Page {Page} generated a {action} recommendation"
                                        , e.Page
                                        , e.Action);

            //allow the firewall to block a request
            e.AllowGuardAction = true;

            if (Debugger.IsAttached)
            {
                //pause the application when debugged to allow you to use debugger to inspect state
                Debugger.Break();
            }
        }

        private void MyFireWall_OnUserTypeChange(object sender, Walter.Web.FireWall.EventArguments.UserTypeChangedEventArgs e)
        {
            _logger.Lazy().LogInformation(eventId: new EventId(Line(), Method())
                                         , message: "Module {Module} detected a user {user} type change from {fromType} to {toType} on {path}"
                                         , e.Module
                                         , e.User
                                         , e.OriginalType
                                         , e.NewType
                                         , e.Url.LocalPath
                                         );
            e.Allow = false;
            if (Debugger.IsAttached)
            {
                //pause the application when debugged to allow you to use debugger to inspect state
                Debugger.Break();
            }

        }

        private void MyFireWall_OnPhishyRequest(object sender, Walter.Web.FireWall.EventArguments.PhishyRequestEventArgs e)
        {
            var fireWallUser = e.Request.User.AsFirewallUser();

            _logger.Lazy().LogInformation(eventId: new EventId(Line(), Method())
                                         , message: "A request by user {fireWallUser} with a search engine status of {SearchEngine} tries to access a non existing resource = {method} {path} via referer = {referer}."
                                         , fireWallUser
                                         , e.Request.User.IsSearchEngine
                                         , e.Request.Method
                                         , e.Request.OriginalUrl.PathAndQuery
                                         , e.Request.PreviousPage?.PathAndQuery ?? e.Request.Referrer?.PathAndQuery ?? "un-known referer"
                                         );

            //set create incident to true if the link does not exist, normally this would be a penetration attempt
            //e.CreateIncident = false;

            e.CreateIncident = e.SiteMapSearch == SiteMapSearchResult.NotFound && e.Request.User.IsSearchEngine == SearchEngine.NotSure;


            if (Debugger.IsAttached)
            {
                //pause the application when debugged to allow you to use debugger to inspect state
                Debugger.Break();
            }
        }

        private void MyFireWall_OnIncident(object sender, Walter.Web.FireWall.EventArguments.FireWallIncidentEventArgs e)
        {

            /* 
             * the Guard will have generated a indecent report in the ILogger instance at Walter.Web.FireWall.Guard
             * see logger settings at appsettings.json
             * 
             * you can configure the log level for incidents at see:
             * RulesConfig.Default.IncidentLogLevel
             * RulesConfig.Default.IncidentEventId
             */


            var si = e.StackEntry;

            _logger?.Lazy().LogInformation(eventId: new EventId(Line(), Method())
                                       , message: "{ControlledBy}: Page Nr {Nr} {method} {path} triggered rule {RuleNr} due to {Reason} will not generate an incident as UserCode has taken responsibility for the request from {country}"
                                       , si.ControlledBy
                                       , e.Page.SessionPageGroupNumber
                                       , e.Page.Method
                                       , e.Page.OriginalUrl.AbsolutePath
                                       , si.RuleNr
                                       , si.Reason
                                       , Walter.BOM.Geo.GeoLocationMapping.GetCountryName(e.Page.Country ?? Walter.BOM.Geo.GeoLocation.UnKnown)
                                       );

            foreach (var entry in e.Data)
            {
                _logger?.Lazy().LogInformation(eventId: new EventId(Line(), Method()), message: "{Type}={Data}", entry.Key, entry.Value);
            }

            e.AllowRaiseIncident = true;

            if (Debugger.IsAttached)
            {
                //pause the application when debugged to allow you to use debugger to inspect state
                Debugger.Break();
            }
        }

        private void MyFireWall_OnCaughtExceptiont(object sender, Walter.Web.FireWall.EventArguments.ExceptionCaughtEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                //pause the application when debugged
                Debugger.Break();
            }
            _logger.Lazy().LogInformation(eventId: new EventId(Line(), Method()), exception: e.Page.Exception, " Request {Page} triggered {} exception {type}"
                    , e.Page
                    , e.ExceptionHandled ? "handled" : "un-handled"
                    , e.Page.Exception.GetType().Name
                    );
        }

        internal static int Line([CallerLineNumber] int line = -1) => line;
        internal static string Method([CallerMemberName] string method = "") => method;
    }
}