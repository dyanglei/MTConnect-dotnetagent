/*
* Copyright (c) 2008, AMT – The Association For Manufacturing Technology (“AMT”)
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of the AMT nor the
*       names of its contributors may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* DISCLAIMER OF WARRANTY. ALL MTCONNECT MATERIALS AND SPECIFICATIONS PROVIDED
* BY AMT, MTCONNECT OR ANY PARTICIPANT TO YOU OR ANY PARTY ARE PROVIDED "AS IS"
* AND WITHOUT ANY WARRANTY OF ANY KIND. AMT, MTCONNECT, AND EACH OF THEIR
* RESPECTIVE MEMBERS, OFFICERS, DIRECTORS, AFFILIATES, SPONSORS, AND AGENTS
* (COLLECTIVELY, THE "AMT PARTIES") AND PARTICIPANTS MAKE NO REPRESENTATION OR
* WARRANTY OF ANY KIND WHATSOEVER RELATING TO THESE MATERIALS, INCLUDING, WITHOUT
* LIMITATION, ANY EXPRESS OR IMPLIED WARRANTY OF NONINFRINGEMENT,
* MERCHANTABILITY, OR FITNESS FOR A PARTICULAR PURPOSE. 

* LIMITATION OF LIABILITY. IN NO EVENT SHALL AMT, MTCONNECT, ANY OTHER AMT
* PARTY, OR ANY PARTICIPANT BE LIABLE FOR THE COST OF PROCURING SUBSTITUTE GOODS
* OR SERVICES, LOST PROFITS, LOSS OF USE, LOSS OF DATA OR ANY INCIDENTAL,
* CONSEQUENTIAL, INDIRECT, SPECIAL OR PUNITIVE DAMAGES OR OTHER DIRECT DAMAGES,
* WHETHER UNDER CONTRACT, TORT, WARRANTY OR OTHERWISE, ARISING IN ANY WAY OUT OF
* THIS AGREEMENT, USE OR INABILITY TO USE MTCONNECT MATERIALS, WHETHER OR NOT
* SUCH PARTY HAD ADVANCE NOTICE OF THE POSSIBILITY OF SUCH DAMAGES.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using MTConnectAgentCore;

namespace MTConnectAgentWindowsService
{
    public partial class MTConnect : ServiceBase
    {
        private Agent agent;
        static private String SERVICENAME = "MTConnect";
        public MTConnect()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists(SERVICENAME))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                   SERVICENAME, "Application");
            }
            eventLog1.Source = SERVICENAME;
            eventLog1.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            agent = new Agent();
            try
            {

                agent.Start();
            }
            catch (AgentException exp)
            {
                String msg = exp.Message;
                if (exp.InnerException != null)
                    msg = msg + "\n" + exp.InnerException.Message;
                eventLog1.WriteEntry(msg, System.Diagnostics.EventLogEntryType.Error);
            }
            catch (System.UnauthorizedAccessException eu)
            {
                eventLog1.WriteEntry("Access denied.  Please specify the user account that the MTConnect service can use to log on.", System.Diagnostics.EventLogEntryType.Error);
                throw eu;
            }
        }

        protected override void OnStop()
        {
            agent.Stop();
        }
    }
}
