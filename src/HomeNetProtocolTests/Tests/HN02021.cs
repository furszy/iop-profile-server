﻿using Google.Protobuf;
using HomeNetCrypto;
using HomeNetProtocol;
using Iop.Homenode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HomeNetProtocolTests.Tests
{
  /// <summary>
  /// HN02021 - Call Identity Application Service - Uninitialized
  /// https://github.com/Internet-of-People/message-protocol/blob/master/TESTS.md#hn02021---call-identity-application-service---uninitialized
  /// </summary>
  public class HN02021 : ProtocolTest
  {
    public const string TestName = "HN02021";
    private static NLog.Logger log = NLog.LogManager.GetLogger("Test." + TestName);

    public override string Name { get { return TestName; } }

    /// <summary>List of test's arguments according to the specification.</summary>
    private List<ProtocolTestArgument> argumentDescriptions = new List<ProtocolTestArgument>()
    {
      new ProtocolTestArgument("Node IP", ProtocolTestArgumentType.IpAddress),
      new ProtocolTestArgument("clNonCustomer Port", ProtocolTestArgumentType.Port),
    };

    public override List<ProtocolTestArgument> ArgumentDescriptions { get { return argumentDescriptions; } }


    /// <summary>
    /// Implementation of the test itself.
    /// </summary>
    /// <returns>true if the test passes, false otherwise.</returns>
    public override async Task<bool> RunAsync()
    {
      IPAddress NodeIp = (IPAddress)ArgumentValues["Node IP"];
      int ClNonCustomerPort = (int)ArgumentValues["clNonCustomer Port"];
      log.Trace("(NodeIp:'{0}',ClNonCustomerPort:{1})", NodeIp, ClNonCustomerPort);

      bool res = false;
      Passed = false;

      ProtocolClient client1 = new ProtocolClient();
      ProtocolClient client2 = new ProtocolClient();
      try
      {
        MessageBuilder mb1 = client1.MessageBuilder;
        MessageBuilder mb2 = client2.MessageBuilder;
        byte[] identityId1 = client1.GetIdentityId();

        // Step 1
        await client1.ConnectAsync(NodeIp, ClNonCustomerPort, true);
        bool homeNodeOk = await client1.EstablishHomeNodeAsync();
        client1.CloseConnection();

        // Step 1 Acceptance
        bool step1Ok = homeNodeOk;


        // Step 2
        await client2.ConnectAsync(NodeIp, ClNonCustomerPort, true);
        bool verifyIdentityOk = await client2.VerifyIdentityAsync();

        Message requestMessage = mb2.CreateCallIdentityApplicationServiceRequest(identityId1, "Test Service");
        await client2.SendMessageAsync(requestMessage);
        Message responseMessage = await client2.ReceiveMessageAsync();

        bool idOk = responseMessage.Id == requestMessage.Id;
        bool statusOk = responseMessage.Response.Status == Status.ErrorUninitialized;
        bool callIdentityOk = idOk && statusOk;

        // Step 2 Acceptance
        bool step2Ok = verifyIdentityOk && callIdentityOk;


        Passed = step1Ok && step2Ok;

        res = true;
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }
      client1.Dispose();
      client2.Dispose();

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}
