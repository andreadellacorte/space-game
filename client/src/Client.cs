// Copyright (c) Improbable Worlds Ltd, All Rights Reserved

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Threading;
using Improbable;
using Improbable.Worker;

namespace Demo
{
  class Client
  {
    private const string WorkerType = "InteractiveClient";
    private const string LoggerName = "Client.cs";
    private const int ErrorExitStatus = 1;
    private const uint GetOpListTimeoutInMilliseconds = 100;
    private const uint CommandRequestTimeoutMS = 100;
    private const int pingIntervalMs = 5000;
    
    private static readonly Random random = new Random();
    private static string playerId;
    private static EntityId planetId;
    
    private static readonly EntityId[] PlanetAuthorityMarkersEntityIds =
    {
      new EntityId(1),
      new EntityId(2),
      new EntityId(3),
      new EntityId(4)
    };

    static int Main(string[] arguments)
    {
      Action printUsage = () =>
      {
        Console.WriteLine("Usage: Client <hostname> <port> <client_id>");
        Console.WriteLine("Connects to a demo deployment.");
        Console.WriteLine("    <hostname>      - hostname of the receptionist to connect to.");
        Console.WriteLine("    <port>          - port to use.");
        Console.WriteLine("    <client_id>     - name of the client.");
      };

      if (arguments.Length != 3)
      {
        printUsage();
        return ErrorExitStatus;
      }

      Console.WriteLine("Client Starting...");
      using (var connection = ConnectClient(arguments))
      {
        using (var dispatcher = new Dispatcher())
        {
          var isConnected = true;

          dispatcher.OnDisconnect(op =>
          {
            Console.Error.WriteLine("[disconnect] {0}", op.Reason);
            isConnected = false;
          });

          dispatcher.OnLogMessage(op =>
          {
            connection.SendLogMessage(op.Level, LoggerName, op.Message);
            Console.WriteLine("Log Message: {0}", op.Message);
            if (op.Level == LogLevel.Fatal)
            {
              Console.Error.WriteLine("Fatal error: {0}", op.Message);
              Environment.Exit(ErrorExitStatus);
            }
          });

          dispatcher.OnAuthorityChange<Position>(cb =>
          {
            Console.WriteLine("authority change {0}", cb.Authority);
          });
          
          dispatcher.OnCommandResponse<AssignPlanetResponder.Commands.AssignPlanet>(response =>
          {
              HandleAssignPlanet(response, connection);
          });

          connection.SendLogMessage(LogLevel.Info, LoggerName,
            "Successfully connected using TCP and the Receptionist");
            
          AssignPlanetResponder.Commands.AssignPlanet.Request assignPlanet =
            new AssignPlanetResponder.Commands.AssignPlanet.Request(new AssignPlanetRequest(playerId));

          connection.SendCommandRequest(PlanetAuthorityMarkersEntityIds[random.Next(PlanetAuthorityMarkersEntityIds.Length)], assignPlanet, CommandRequestTimeoutMS, null);
          
          // UX Thread to read from CLI
          new Thread(() =>
          {
            while (isConnected)
            {
              Thread.CurrentThread.IsBackground = true;
              string s = Console.ReadLine();
              Console.WriteLine("Please enter your command:");
              Console.WriteLine("1. Build mine");
              
              if(s == "1")
              {
                Console.WriteLine("PRRR, new mine built.");
              }
              else
              {
                Console.WriteLine("No idea about that command, sorry.");
              }
            }
          }).Start();
          
          // Main loop to read from SpatialOS
          while (isConnected)
          {
            var opList = connection.GetOpList(GetOpListTimeoutInMilliseconds);
            dispatcher.Process(opList);
          }
        }
      }

      return 0;
    }

    private static Connection ConnectClient(string[] arguments)
    {
      string hostname = arguments[0];
      ushort port = Convert.ToUInt16(arguments[1]);
      playerId = arguments[2];
      var connectionParameters = new ConnectionParameters();
      connectionParameters.WorkerType = WorkerType;
      connectionParameters.Network.ConnectionType = NetworkConnectionType.Tcp;

      using (var future = Connection.ConnectAsync(hostname, port, playerId, connectionParameters))
      {
        return future.Get();
      }
    }
    
    private static void HandleAssignPlanet(CommandResponseOp<AssignPlanetResponder.Commands.AssignPlanet> response, Connection connection)
    {
      if (response.StatusCode != StatusCode.Success)
      {
        StringBuilder logMessageBuilder = new StringBuilder();
        logMessageBuilder.Append(
            String.Format("Received invalid OnCommandResponse for request ID {0} with status code {1} to entity with ID {2}.", response.RequestId, response.StatusCode, response.EntityId));
        if (!string.IsNullOrEmpty(response.Message))
        {
            logMessageBuilder.Append(String.Format("The message was \'{0}\'.", response.Message));
        }

        if (!response.Response.HasValue)
        {
            logMessageBuilder.Append("The response was missing.");
        }
        else
        {
            logMessageBuilder.Append(
                String.Format("The EntityIdResponse ID value was {0}", response.Response.Value.Get().Value));
        }

        connection.SendLogMessage(LogLevel.Warn, LoggerName, logMessageBuilder.ToString());
      }
      else
      {
        planetId = response.Response.Value.Get().Value.planetId;
        var logMessage = String.Format("Assigned Planet with EntityId {0} to this client", planetId.Id);
      
        Console.WriteLine(logMessage);
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      }
    }
  }
}
