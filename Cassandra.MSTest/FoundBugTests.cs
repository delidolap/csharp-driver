//
//      Copyright (C) 2012 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if MYTEST
using MyTest;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cassandra.MSTest;
#endif

namespace Cassandra.MSTest
{
    [TestClass]
    public class FoundBugTests
    {
        [TestMethod]
        [WorksForMe]
        public void Jira_CSHARP_40()
        //During reconnect the tablespace name becomes invalid
        {
            var CCMCluster = CCMBridge.CCMCluster.Create(2, Cluster.Builder());
            try
            {
                var Session = CCMCluster.Session;
                var Keyspace = "Excelsior";
                Session.CreateKeyspaceIfNotExists(Keyspace);
                Session.ChangeKeyspace(Keyspace);
                const string cqlKeyspaces = "SELECT * from system.schema_keyspaces";
                var query = new SimpleStatement(cqlKeyspaces).EnableTracing();
                {
                    using (var result = Session.Execute(query))
                        foreach (var resKeyspace in result.GetRows())
                        {
                            Console.WriteLine("durable_writes={0} keyspace_name={1} strategy_Class={2} strategy_options={3}",
                                              resKeyspace.GetValue<bool>("durable_writes"),
                                              resKeyspace.GetValue<string>("keyspace_name"),
                                              resKeyspace.GetValue<string>("strategy_class"),
                                              resKeyspace.GetValue<string>("strategy_options"));
                        }
                }

                CCMCluster.CCMBridge.ForceStop(1);
                CCMCluster.CCMBridge.ForceStop(2);
                TestUtils.waitForDown(Options.Default.IP_PREFIX + "1", CCMCluster.Cluster, 40);
                TestUtils.waitForDown(Options.Default.IP_PREFIX + "2", CCMCluster.Cluster, 40);

                try
                {
                    using (var result = Session.Execute(query))
                        foreach (var resKeyspace in result.GetRows())
                        {
                            Console.WriteLine(resKeyspace.GetValue<string>("keyspace_name"));
                        }
                }
                catch (Exception)
                {
                }

                CCMCluster.CCMBridge.Start(1);
                TestUtils.waitFor(Options.Default.IP_PREFIX + "1", CCMCluster.Cluster, 60);

                {
                    var result = Session.Execute(query);

                    foreach (var resKeyspace in result.GetRows())
                    {
                        Console.WriteLine("durable_writes={0} keyspace_name={1} strategy_Class={2} strategy_options={3}",
                                          resKeyspace.GetValue<bool>("durable_writes"),
                                          resKeyspace.GetValue<string>("keyspace_name"),
                                          resKeyspace.GetValue<string>("strategy_class"),
                                          resKeyspace.GetValue<string>("strategy_options"));
                    }
                }

            }
            finally
            {
                CCMCluster.Discard();
            }
        }
    }
}
