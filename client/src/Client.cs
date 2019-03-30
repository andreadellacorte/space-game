// Copyright (c) Improbable Worlds Ltd, All Rights Reserved

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Threading;
using Improbable;
using Improbable.Worker;
using Improbable.Worker.Alpha;

namespace Demo
{
  class Client
  {
    private const string DAT_TokenSecret = "MWY3MTUyNjAtYmY1MS00MzRjLThiMmUtOTc4YTBjOGQ3NWJlOjo4M2ZlOGE3My0xZjFjLTQ3MmEtOGRmNi03NGM1ZDdkNDk2MTg=";
    private const string WorkerType = "LauncherClient";
    private const string LoggerName = "Client.cs";
    private const int ErrorExitStatus = 1;
    private const uint GetOpListTimeoutInMilliseconds = 0;
    private const uint CommandRequestTimeoutMS = 0;
    
    private static readonly Random random = new Random();
    private static string playerId;
    private static EntityId assignedPlanetId;
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
        Console.WriteLine("Connects to a local deployment.");
        Console.WriteLine("    <hostname>      - hostname of the receptionist to connect to.");
        Console.WriteLine("    <port>          - port to use.");
        Console.WriteLine("    <client_id>     - name of the client.");
      };

      if (arguments.Length != 1 && arguments.Length != 3)
      {
        printUsage();
        return ErrorExitStatus;
      }
      
      Console.Clear();

      Console.ForegroundColor = ConsoleColor.White;
      Console.BackgroundColor = ConsoleColor.DarkBlue;
      Console.WriteLine("Welcome to space-game!");
      Console.WriteLine("======================");
      Console.WriteLine();
      Console.WriteLine("https://github.com/andreadellacorte/space-game");
      Console.WriteLine("==============================================");
      Console.WriteLine();
      Console.ResetColor();

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
          
          dispatcher.OnCommandResponse<PlanetImprovementResponder.Commands.PlanetImprovement>(response =>
          {
              HandlePlanetImprovementResponse(response, connection);
          });

          connection.SendLogMessage(LogLevel.Info, LoggerName,
            "Successfully connected using TCP and the Receptionist");
          
          // UX Thread to read from CLI
          new Thread(() =>
          {
            while (isConnected)
            {
              Thread.CurrentThread.IsBackground = true;
            
              // Finding a planet for the client
              while(!assignedPlanetId.IsValid())
              {
                displayProgressBar("Assigning you a planet... ", 10);

                AssignPlanetResponder.Commands.AssignPlanet.Request assignPlanet =
                  new AssignPlanetResponder.Commands.AssignPlanet.Request(new AssignPlanetRequest(playerId));

                connection.SendCommandRequest(PlanetAuthorityMarkersEntityIds[random.Next(PlanetAuthorityMarkersEntityIds.Length)], assignPlanet, 1500, null);
                
                System.Threading.Thread.Sleep(1600);
              }
              
              // Start user interaction loop
              Console.ForegroundColor = ConsoleColor.DarkRed;
              Console.WriteLine("Available commands:");
              Console.WriteLine();
              Console.ResetColor();
              
              Console.Write("\ti, improve <improv>");
              Console.ForegroundColor = ConsoleColor.DarkYellow;
              Console.WriteLine("\t\timprove the planet's facilities");
              Console.ResetColor();
              
              Console.Write("\tb, build <ship>");
              Console.ForegroundColor = ConsoleColor.DarkYellow;
              Console.WriteLine("\t\t\tbuild a ship or other implements");
              Console.ResetColor();
              
              Console.Write("\tr, research <research>");
              Console.ForegroundColor = ConsoleColor.DarkYellow;
              Console.WriteLine("\t\timprove the planet's technology");
              Console.ResetColor();
              
              Console.Write("\ts, scan <planetId>");
              Console.ForegroundColor = ConsoleColor.DarkYellow;
              Console.WriteLine("\t\tscan a planet (leave planetId empty to scan your planet, uses 1 probe)");
              Console.ResetColor();
              
              Console.Write("\tq, quit");
              Console.ForegroundColor = ConsoleColor.DarkYellow;
              Console.WriteLine("\t\t\t\texit the game");
              Console.ResetColor();
              
              Console.WriteLine();
              Console.ForegroundColor = ConsoleColor.DarkRed;
              Console.Write("Command: ");
              Console.ResetColor();

              string input = Console.ReadLine().Trim();
              
              Dictionary<string, Improvement>  stringToImprovements = new Dictionary<string, Improvement>();
              stringToImprovements.Add("mine", Improvement.MINE);
              stringToImprovements.Add("m", Improvement.MINE);
              stringToImprovements.Add("deposit", Improvement.DEPOSIT);
              stringToImprovements.Add("d", Improvement.DEPOSIT);
              stringToImprovements.Add("hangar", Improvement.HANGAR);
              stringToImprovements.Add("h", Improvement.HANGAR);
              
              Dictionary<string, Improvement>  stringToShips = new Dictionary<string, Improvement>();
              stringToShips.Add("probe", Improvement.PROBE);
              stringToShips.Add("p", Improvement.PROBE);
              
              Dictionary<string, Improvement>  stringToResearch = new Dictionary<string, Improvement>();
              stringToResearch.Add("nanobots", Improvement.NANOBOTS);
              stringToResearch.Add("n", Improvement.NANOBOTS);
              
              string[] command = input.Split(' ');
              
              Console.WriteLine();
              
              switch(command[0])
              {
                case "i":
                case "improve":
                  if(command.Length == 2 && stringToImprovements.ContainsKey(command[1]))
                  {
                    displayProgressBar($"Improving '{stringToImprovements[command[1]]}'...", 10);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("Command sent: ");
                    Console.WriteLine(input);
                    Console.ResetColor();
                    Console.WriteLine();
                    PlanetImprovementResponder.Commands.PlanetImprovement.Request planetImprovement =
                      new PlanetImprovementResponder.Commands.PlanetImprovement.Request(new PlanetImprovementRequest(assignedPlanetId, stringToImprovements[command[1]]));
                    connection.SendCommandRequest(assignedPlanetId, planetImprovement, CommandRequestTimeoutMS, null);
                  }
                  else
                  {
                    displayProgressBar("Checking... ", 10);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("Command sent: ");
                    Console.ResetColor();
                    Console.WriteLine(input);
                    Console.WriteLine();
                    
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("Command usage: ");
                    Console.ResetColor();
                    Console.Write("i, improve <improv>");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\timprove the planet's facilities");
                    Console.ResetColor();
                    
                    Console.WriteLine();

                    Console.Write("\tm, mine");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\t\t\t\tmakes more minerals");
                    Console.ResetColor();
                    
                    Console.Write("\td, deposit");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\t\t\tstore more minerals");
                    Console.ResetColor();

                    Console.Write("\th, hangar");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\t\t\tstore more ships");
                    Console.ResetColor();
                  }
                  break;
                case "b":
                case "build":
                  if(command.Length == 2 && stringToShips.ContainsKey(command[1]))
                  {
                    displayProgressBar($"Building a '{stringToShips[command[1]]}'", 10);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("Command sent: ");
                    Console.ResetColor();
                    Console.WriteLine(input);
                    Console.WriteLine();
                    PlanetImprovementResponder.Commands.PlanetImprovement.Request planetImprovement =
                      new PlanetImprovementResponder.Commands.PlanetImprovement.Request(new PlanetImprovementRequest(assignedPlanetId, stringToShips[command[1]]));
                    connection.SendCommandRequest(assignedPlanetId, planetImprovement, CommandRequestTimeoutMS, null);
                  }
                  else
                  {
                    displayProgressBar("Checking... ", 10);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("Command sent: ");
                    Console.ResetColor();
                    Console.WriteLine(input);
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("Command usage: ");
                    Console.ResetColor();
                    Console.Write("b, build <ship>");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\t\tbuild a ship or other implements");
                    Console.ResetColor();
                    
                    Console.WriteLine();

                    Console.Write("\tp, probe");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\t\t\tcan be used to scan other planets");
                    Console.ResetColor();
                  }
                  break;
                  case "r":
                  case "research":
                    if(command.Length == 2 && stringToResearch.ContainsKey(command[1]))
                    {
                      displayProgressBar($"Researching '{stringToResearch[command[1]]}'", 10);
                      Console.ForegroundColor = ConsoleColor.DarkRed;
                      Console.Write("Command sent: ");
                      Console.ResetColor();
                      Console.WriteLine(input);
                      Console.WriteLine();
                      PlanetImprovementResponder.Commands.PlanetImprovement.Request planetImprovement =
                        new PlanetImprovementResponder.Commands.PlanetImprovement.Request(new PlanetImprovementRequest(assignedPlanetId, stringToResearch[command[1]]));
                      connection.SendCommandRequest(assignedPlanetId, planetImprovement, CommandRequestTimeoutMS, null);
                    }
                    else
                    {
                      displayProgressBar("Checking... ", 10);
                      Console.ForegroundColor = ConsoleColor.DarkRed;
                      Console.Write("Command sent: ");
                      Console.ResetColor();
                      Console.WriteLine(input);
                      Console.WriteLine();

                      Console.ForegroundColor = ConsoleColor.DarkGreen;
                      Console.Write("Command Usage: ");
                      Console.ResetColor();
                      Console.Write("r, research <res>");
                      Console.ForegroundColor = ConsoleColor.DarkYellow;
                      Console.WriteLine("\timprove the planet's technology");
                      Console.ResetColor();
                      
                      Console.WriteLine();

                      Console.Write("\tn, nanobots");
                      Console.ForegroundColor = ConsoleColor.DarkYellow;
                      Console.WriteLine("\t\t\tbuild everything faster");
                      Console.ResetColor();
                    }
                    break;
                case "s":
                case "scan":
                case "":
                  long i = assignedPlanetId.Id;
                  if (command.Length == 2 && !Int64.TryParse(command[1], out i))
                  {
                    Console.Write($"Cannot scan Planet with EntityId: '{command[1]}', sorry.");
                    break;
                  }
                  EntityId planetToScan = new EntityId(i);

                  displayProgressBar("Checking planet status... ", 10);
                  Console.ForegroundColor = ConsoleColor.DarkRed;
                  Console.Write("Command sent: ");
                  Console.ResetColor();
                  Console.WriteLine(input);
                  Console.WriteLine();

                  // TODO NEED TO USE PROBE AND LIMIT INFORMATION FOR FOREIGN PLANETS

                  PlanetInfoResponder.Commands.PlanetInfo.Request planetInfo =
                    new PlanetInfoResponder.Commands.PlanetInfo.Request(new PlanetInfoRequest(planetToScan));
                  connection.SendCommandRequest(planetToScan, planetInfo, CommandRequestTimeoutMS, null);
                  break;
                case "q":
                case "quit":
                  displayProgressBar("Quitting... ", 10);
                  Console.ForegroundColor = ConsoleColor.DarkRed;
                  Console.Write("Command sent: ");
                  Console.ResetColor();
                  Console.WriteLine(input);
                  Console.WriteLine();
                  isConnected = false;
                  break;
                default:
                  displayProgressBar("Checking... ", 10);
                  Console.ForegroundColor = ConsoleColor.DarkRed;
                  Console.Write("Command sent: ");
                  Console.ResetColor();
                  Console.WriteLine(input);
                  Console.WriteLine();
                  Console.ForegroundColor = ConsoleColor.DarkRed;
                  Console.WriteLine("No idea about that command, sorry.");
                  Console.ResetColor();
                  break;
                }
                Console.WriteLine();
                System.Threading.Thread.Sleep(1500);
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
    
    private static void displayProgressBar(string waitMessage, int waitLength)
    {
      Console.Write(waitMessage);
      using (var progress = new ProgressBar()) {
        for (int i = 0; i <= 100; i++) {
          progress.Report((double) i / 100);
          Thread.Sleep(waitLength);
        }
      }
    }

    private static Connection ConnectClient(string[] arguments)
    {
      playerId = arguments[0];
      if(arguments.Length == 3)
      {
        string hostname = arguments[1];
        ushort port = Convert.ToUInt16(arguments[2]);
        var connectionParameters = new ConnectionParameters();
        connectionParameters.WorkerType = WorkerType;
        connectionParameters.Network.ConnectionType = NetworkConnectionType.Tcp;

        using (var future = Connection.ConnectAsync(hostname, port, playerId, connectionParameters))
        {
          return future.Get();
        }
      }
      else
      {
        const string LocatorServerAddress = "locator.improbable.io";
        const int LocatorServerPort = 444;
        
        var pitFuture = DevelopmentAuthentication.CreateDevelopmentPlayerIdentityTokenAsync(LocatorServerAddress, LocatorServerPort,
          new PlayerIdentityTokenRequest
          {
              DevelopmentAuthenticationTokenId = DAT_TokenSecret,
              PlayerId = playerId,
              DisplayName = "Andrea",
              Metadata = ""
          });
        PlayerIdentityTokenResponse playerIdentityTokenResponse = pitFuture.Get();
        
        var ltFuture = DevelopmentAuthentication.CreateDevelopmentLoginTokensAsync(LocatorServerAddress, LocatorServerPort,
          new LoginTokensRequest
          {
              PlayerIdentityToken = playerIdentityTokenResponse.PlayerIdentityToken,
              WorkerType = WorkerType
          });
        LoginTokensResponse loginTokensResponse = ltFuture.Get();
        
        if (loginTokensResponse.LoginTokens.Count == 0){
          throw new SystemException("No running deployments with dev_login tag");
        }
                
        var locatorParameters = new Improbable.Worker.Alpha.LocatorParameters
        {
          PlayerIdentity = new PlayerIdentityCredentials
          {
              PlayerIdentityToken = playerIdentityTokenResponse.PlayerIdentityToken,
              LoginToken = loginTokensResponse.LoginTokens[0].LoginToken
          }
        };
        var locator = new Improbable.Worker.Alpha.Locator(LocatorServerAddress, LocatorServerPort, locatorParameters);
        using (var connectionFuture = locator.ConnectAsync(new ConnectionParameters
        {
          WorkerType = WorkerType,
          Network = {ConnectionType = NetworkConnectionType.Tcp, UseExternalIp = true}
        }))
        {
          var connection = connectionFuture.Get(Convert.ToUInt32(Defaults.ConnectionTimeoutMillis));
          if (!connection.HasValue || !connection.Value.IsConnected) throw new Exception("No connection or connection not established");
          Console.WriteLine($"Assigned worker ID: {connection.Value.GetWorkerId()}");
          return connectionFuture.Get();
        }
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
        assignedPlanetId = response.Response.Value.Get().Value.planetId;
        planetName = response.Response.Value.Get().Value.planetName;

        var logMessage = String.Format("Assigned Planet '{0}' (EntityId {1}) to this client", planetName, assignedPlanetId.Id);
      
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(logMessage);
        Console.ResetColor();
        Console.WriteLine();
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
      else
      {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        var logMessage = String.Format("Planet '{0}':", response.Response.Value.Get().Value.name);
        Console.WriteLine(logMessage);
        
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        logMessage = String.Format("  / Production: Level {0} mine - Minerals: {1} / {2} max",
          response.Response.Value.Get().Value.mineLevel,
          (int) response.Response.Value.Get().Value.minerals,
          response.Response.Value.Get().Value.depositLevel * 100);
        Console.WriteLine(logMessage);
        
        logMessage = String.Format("  / Spaceships: {0} probes (level {1}) - Total: {2} / {3} max ",
          response.Response.Value.Get().Value.probes,
          0,
          response.Response.Value.Get().Value.probes,
          response.Response.Value.Get().Value.hangarLevel * 3);
        Console.WriteLine(logMessage);
        
        logMessage = String.Format("  / Storage: Level {0} hangar, Level {1} mineral deposit",
          response.Response.Value.Get().Value.hangarLevel,
          response.Response.Value.Get().Value.depositLevel);
        Console.WriteLine(logMessage);
        
        logMessage = String.Format("  / Tech: Level {0} nanobots",
          response.Response.Value.Get().Value.nanobotLevel);
        Console.WriteLine(logMessage);
        
        logMessage = String.Format("  / Build Queue: {0} - {1} seconds remaining",
          response.Response.Value.Get().Value.buildQueue,
          (int) response.Response.Value.Get().Value.buildQueueTime);
        Console.WriteLine(logMessage);
        Console.WriteLine();
        Console.ResetColor();

        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      }
    }
    
    private static void HandlePlanetImprovementResponse(CommandResponseOp<PlanetImprovementResponder.Commands.PlanetImprovement> response, Connection connection)
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
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(response.Response.Value.Get().Value.message);
        Console.WriteLine();
        Console.ResetColor();
        connection.SendLogMessage(LogLevel.Info, LoggerName, response.Response.Value.Get().Value.message);
      }
    }
  }
}
