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
    private static string planetName;
    
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
              HandleAssignPlanetResponse(response, connection);
          });
          
          dispatcher.OnCommandResponse<PlanetInfoResponder.Commands.PlanetInfo>(response =>
          {
              HandlePlanetInfoResponse(response, connection);
          });

          connection.SendLogMessage(LogLevel.Info, LoggerName,
            "Successfully connected using TCP and the Receptionist");
          
          // UX Thread to read from CLI
          new Thread(() =>
          {
            while (isConnected)
            {
              Thread.CurrentThread.IsBackground = true;
              
              // Find a planet
              while(!planetId.IsValid())
              {
                Console.WriteLine("Assigning you a planet... Looking...");

                AssignPlanetResponder.Commands.AssignPlanet.Request assignPlanet =
                  new AssignPlanetResponder.Commands.AssignPlanet.Request(new AssignPlanetRequest(playerId));

                connection.SendCommandRequest(PlanetAuthorityMarkersEntityIds[random.Next(PlanetAuthorityMarkersEntityIds.Length)], assignPlanet, CommandRequestTimeoutMS, null);
                
                System.Threading.Thread.Sleep(1500);
              }
              
              // Start user interaction loop
              string s = Console.ReadLine();
              Console.WriteLine("Please enter your command:");
              Console.WriteLine(" 1. Build mine");
              Console.WriteLine(" 2. Check planet status");
              
              if(s == "1")
              {
                Console.WriteLine("PRRR, new mine built.");
              }
              else if(s == "2")
              {
                Console.WriteLine("Checking planet status...");
                PlanetInfoResponder.Commands.PlanetInfo.Request planetInfo =
                  new PlanetInfoResponder.Commands.PlanetInfo.Request(new PlanetInfoRequest(planetId));
                
                foreach (var entityId in PlanetAuthorityMarkersEntityIds)
                {
                  connection.SendCommandRequest(entityId, planetInfo, CommandRequestTimeoutMS, null);
                }
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
    
    private static void HandleAssignPlanetResponse(CommandResponseOp<AssignPlanetResponder.Commands.AssignPlanet> response, Connection connection)
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
        planetName = response.Response.Value.Get().Value.planetName;

        var logMessage = String.Format("Assigned Planet '{0}' (EntityId {1}) to this client", planetName, planetId.Id);
      
        Console.WriteLine(logMessage);
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      }
    }
    
    private static void HandlePlanetInfoResponse(CommandResponseOp<PlanetInfoResponder.Commands.PlanetInfo> response, Connection connection)
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
      else if(string.IsNullOrEmpty(response.Response.Value.Get().Value.name))
      {
        return;
      }
      else
      {
        var logMessage = String.Format("Received PlanetInfo from '{0}': {1} minerals",
          response.Response.Value.Get().Value.name,
          response.Response.Value.Get().Value.minerals);
      
        Console.WriteLine(logMessage);
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      }
    }
  }
}
