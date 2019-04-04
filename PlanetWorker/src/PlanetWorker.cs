// Copyright (c) Improbable Worlds Ltd, All Rights Reserved

using System;
using System.Reflection;
using System.Collections.Generic;
using Improbable;
using Improbable.Worker;
using Improbable.Collections;

namespace Demo
{
  class PlanetWorker
  {
    private const int economySpeed = 1;
    private const uint timeoutMillis = 500u;
    private const string WorkerType = "PlanetWorker";
    private const string LoggerName = "PlanetWorker.cs";
    private const int ErrorExitStatus = 1;
    private const uint GetOpListTimeoutInMilliseconds = 0;
    private const double SecondsPerFrame = 0.2;

    private static Dictionary<EntityId, Entity> EntityMap = new Dictionary<EntityId, Entity>();
    
    private static bool isPlanet(Entity entity)
    {
      return entity.GetComponentIds().Contains(PlanetInfo.ComponentId);
    }

    static int Main(string[] arguments)
    {
      Action printUsage = () =>
      {
        Console.WriteLine("Usage: " + WorkerType + " <hostname> <port> <worker_id>");
        Console.WriteLine("Connects to the deployment.");
        Console.WriteLine("    <hostname>      - hostname of the receptionist to connect to.");
        Console.WriteLine("    <port>          - port to use.");
        Console.WriteLine("    <worker_id>     - name of the worker assigned by SpatialOS.");
      };

      if (arguments.Length < 3)
      {
        printUsage();
        return ErrorExitStatus;
      }

      Console.WriteLine("Worker Starting...");
      
      using (var connection = ConnectWorker(arguments))
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
          
          dispatcher.OnAuthorityChange<PlanetInfo>(op =>
          {
            Entity entity;
            if (EntityMap.TryGetValue(op.EntityId, out entity)
                && (op.Authority == Authority.NotAuthoritative
                    || op.Authority == Authority.AuthorityLossImminent))
            {
              EntityMap.Remove(op.EntityId);
            }
          });
          
          dispatcher.OnAddComponent<PlanetInfo>(op =>
          {
            //var logMessage = String.Format("Adding PlanetInfo Component for entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            Entity entity;
            if (EntityMap.TryGetValue(op.EntityId, out entity))
            {
              entity.Add<PlanetInfo>(op.Data);
            }
            else
            {
              Entity newEntity = new Entity();
              EntityMap.Add(op.EntityId, entity);
              entity.Add<PlanetInfo>(op.Data);
            }
          });
          
          dispatcher.OnComponentUpdate<PlanetInfo>(op=>
          {
            //var logMessage = String.Format("Updating PlanetInfo Component for entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            Entity entity;
            if (EntityMap.TryGetValue(op.EntityId, out entity))
            {
                var update = op.Update.Get();
                entity.Update<PlanetInfo>(update);
            }
          });
          
          dispatcher.OnAddEntity(op =>
          {
            //var logMessage = String.Format("Adding entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            // AddEntity will always be followed by OnAddComponent
            Entity newEntity = new Entity();
            EntityMap.Add(op.EntityId, newEntity);
          });
          
          dispatcher.OnRemoveEntity(op =>
          {
            //var logMessage = String.Format("Removing entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
            EntityMap.Remove(op.EntityId);
          });

          dispatcher.OnCommandRequest<AssignPlanetResponder.Commands.AssignPlanet>(request =>
          {
            HandleAssignPlanetRequest(request, connection);
          });
          
          dispatcher.OnCommandRequest<PlanetInfoResponder.Commands.PlanetInfo>(request =>
          {
            HandlePlanetInfoRequest(request, connection);
          });
          
          dispatcher.OnCommandRequest<PlanetImprovementResponder.Commands.PlanetImprovement>(request =>
          {
            HandlePlanetImprovementRequest(request, connection);
          });

          connection.SendLogMessage(LogLevel.Info, LoggerName,
            "Successfully connected using TCP and the Receptionist");
            
          var maxWait = System.TimeSpan.FromMilliseconds(1000f * SecondsPerFrame);
          var stopwatch = new System.Diagnostics.Stopwatch();

          while (isConnected)
          {
            // Setup Timer
            stopwatch.Reset();
            stopwatch.Start();
            
            var opList = connection.GetOpList(GetOpListTimeoutInMilliseconds);
            dispatcher.Process(opList);
            
            foreach (var pair in EntityMap)
            {
              Entity entity = pair.Value;

              if(!isPlanet(entity))
              {
                continue;
              }
              
              EntityId planetId = pair.Key;
              PlanetInfoData planetInfoData = entity.Get<PlanetInfo>().Value.Get().Value;

              if(planetInfoData.playerId == "") // Don't do this update if the planet is uninhabited)
              {
                continue;
              }

              // Create new component update object
              PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
                                              
              if(planetInfoData.buildQueue != Improvement.EMPTY)
              {
                if(planetInfoData.buildQueueTime - SecondsPerFrame <= 0)
                {
                  switch (planetInfoData.buildQueue)
                  {
                    case Improvement.MINE:
                      planetInfoUpdate.SetMineLevel(planetInfoData.mineLevel + 1);
                      break;
                    case Improvement.PROBE:
                      planetInfoUpdate.SetProbes(planetInfoData.probes+1);
                      break;
                    case Improvement.HANGAR:
                      planetInfoUpdate.SetHangarLevel(planetInfoData.hangarLevel + 1);
                      break;
                    case Improvement.DEPOSIT:
                      planetInfoUpdate.SetDepositLevel(planetInfoData.depositLevel + 1);
                      break;
                    case Improvement.NANOBOTS:
                      planetInfoUpdate.SetNanobotLevel(planetInfoData.nanobotLevel + 1);
                      break;
                    default:
                      throw new SystemException("Unknown improvement type");
                  }
                  
                  planetInfoUpdate.SetBuildQueue(Improvement.EMPTY);
                  planetInfoUpdate.SetBuildQueueTime(0);
                }
                else
                {
                  planetInfoUpdate.SetBuildQueueTime(planetInfoData.buildQueueTime - SecondsPerFrame);
                }
              }

              if(planetInfoData.buildMaterials > 0)
              {
                planetInfoUpdate.SetMinerals(planetInfoData.minerals - planetInfoData.buildMaterials);
                planetInfoUpdate.SetBuildMaterials(0);
              }
              else
              {
                if(planetInfoData.minerals < planetInfoData.depositLevel * 100)
                {
                  planetInfoUpdate.SetMinerals(planetInfoData.minerals + planetInfoData.mineLevel * SecondsPerFrame);
                }
              }
              
              connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);
            }
            
            // Wait for a bit if necessary
            stopwatch.Stop();
            var waitFor = maxWait.Subtract(stopwatch.Elapsed);
            System.Threading.Thread.Sleep(waitFor.Milliseconds > 0 ? waitFor : System.TimeSpan.Zero);
          }
        }
      }

      return 0;
    }

    private static Connection ConnectWorker(string[] arguments)
    {
      string hostname = arguments[0];
      ushort port = Convert.ToUInt16(arguments[1]);
      string workerId = arguments[2];
      var connectionParameters = new ConnectionParameters();
      connectionParameters.WorkerType = WorkerType;
      connectionParameters.Network.ConnectionType = NetworkConnectionType.Tcp;

      using (var future = Connection.ConnectAsync(hostname, port, workerId, connectionParameters))
      {
        return future.Get();
      }
    }
    
    private static void HandleAssignPlanetRequest(CommandRequestOp<AssignPlanetResponder.Commands.AssignPlanet> request, Connection connection)
    {
      var logMessage = String.Format("Received AssignPlanet command from player {0}", request.Request.Get().Value.playerId);
      connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

      string planetName = "";
      PlanetInfoData planetInfoData;
      var playerId = request.Request.Get().Value.playerId;
      var planetId = request.Request.Get().Value.planetId;
      var planetPassword = request.Request.Get().Value.password;
      
      Entity entity;
      if(planetId.Id != 0)
      {
        if(EntityMap.TryGetValue(planetId, out entity))
        {
          if(!isPlanet(entity))
          {
            planetId = new EntityId(0);
            logMessage = "Not a planet";
          }
          else
          {
            planetInfoData = entity.Get<PlanetInfo>().Value.Get().Value;
            if(planetInfoData.password != planetPassword)
            {
              planetId = new EntityId(0);
              logMessage = "Password not valid";
            }
            else
            {
              planetName = planetInfoData.name;

              // Create new component update object
              PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
              planetInfoUpdate.SetPlayerId(playerId);
              connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);
            }
          }
        }
        else
        {
          planetId = new EntityId(-1);
        }
      }
      else
      {
        foreach(KeyValuePair<EntityId, Entity> pair in EntityMap)
        {
          if(!isPlanet(pair.Value))
          {
            logMessage = String.Format("Skipping entity with entityId {0}", pair.Key);
            connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
            continue;
          }
          
          planetInfoData = pair.Value.Get<PlanetInfo>().Value.Get().Value;

          logMessage = String.Format("Picked planet with entityId {0} because I have authority", pair.Key);
          connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

          logMessage = String.Format("Entity {0} has playerId {1}", pair.Key, planetInfoData.playerId);
          connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

          if(planetInfoData.playerId == "")
          {
            planetId = pair.Key;
            planetPassword = planetInfoData.password;
            planetName = planetInfoData.name;

            // Create new component update object
            PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
            planetInfoUpdate.SetMinerals(10);
            planetInfoUpdate.SetPlayerId(playerId);
            connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);

            break;
          }
        }
      }

      // Send the assigned planet to the client
      var assignPlanetResponse = new AssignPlanetResponse(planetId, planetName, planetPassword, logMessage);
      var commandResponse = new AssignPlanetResponder.Commands.AssignPlanet.Response(assignPlanetResponse);
      connection.SendCommandResponse(request.RequestId, commandResponse);
      
      if(planetId.Id == 0)
      {
        logMessage = String.Format("HandleAssignPlanetRequest failed for {0}", request.Request.Get().Value.playerId);
        connection.SendLogMessage(LogLevel.Error, LoggerName, logMessage);
      }
    }

    private static void HandlePlanetInfoRequest(CommandRequestOp<PlanetInfoResponder.Commands.PlanetInfo> request, Connection connection)
    {
      var logMessage = String.Format("Received PlanetInfo Command for EntityId {0}", request.Request.Get().Value.planetId);
      connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      
      Entity entity;
      if (EntityMap.TryGetValue(request.Request.Get().Value.planetId, out entity))
      {
        PlanetInfoData planetInfoData = entity.Get<PlanetInfo>().Value.Get().Value;
        var planetInfoResponse = new PlanetInfoResponse(planetInfoData.name,
          planetInfoData.mineLevel, planetInfoData.minerals, planetInfoData.depositLevel,
          planetInfoData.probes, planetInfoData.hangarLevel,
          planetInfoData.nanobotLevel,
          planetInfoData.buildQueue, planetInfoData.buildQueueTime);
        var commandResponse = new PlanetInfoResponder.Commands.PlanetInfo.Response(planetInfoResponse);
        connection.SendCommandResponse(request.RequestId, commandResponse);
      }
      else
      {
        logMessage = String.Format("No planets available for planetId {0}", request.Request.Get().Value.planetId);
        throw new SystemException(logMessage);
      }
    }

    private static void HandlePlanetImprovementRequest(CommandRequestOp<PlanetImprovementResponder.Commands.PlanetImprovement> request, Connection connection)
    {
      var logMessage = String.Format("Received PlanetImprovement Command for EntityId {0}", request.Request.Get().Value.planetId);
      connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      
      Entity entity;
      if (EntityMap.TryGetValue(request.Request.Get().Value.planetId, out entity))
      {
        var planetId = request.Request.Get().Value.planetId;
        PlanetInfoData planetInfoData = entity.Get<PlanetInfo>().Value.Get().Value;
        
        string response;
        int mineralsCost;
        int timeRequired;
        
        if(planetInfoData.buildQueue != Improvement.EMPTY)
        {
          response = String.Format("Build queue is already full; currently building: {0} ({1} seconds left)",
            planetInfoData.buildQueue,
            (int) planetInfoData.buildQueueTime);
        }
        else
        {
          switch(request.Request.Get().Value.improvement)
          {
            case Improvement.MINE:
              mineralsCost = 20 * (int) Math.Pow(1.5, (double) planetInfoData.mineLevel);
              break;
            case Improvement.PROBE:
              mineralsCost = 80;
              break;
            case Improvement.DEPOSIT:
              mineralsCost = 50 * (int) Math.Pow(1.2, (double) planetInfoData.depositLevel);
              break;
            case Improvement.HANGAR:
              mineralsCost = 80 * (int) Math.Pow(1.2, (double) planetInfoData.hangarLevel);
              break;
            case Improvement.NANOBOTS:
              mineralsCost = 300 * (int) Math.Pow(1.5, (double) planetInfoData.nanobotLevel);
              break;
            default:
              throw new SystemException("Unknown improvement type");
          }
          
          if(planetInfoData.minerals < mineralsCost)
          {
            response = String.Format("Not enough minerals to build {0}; requires {1} minerals ({2} available)",
              request.Request.Get().Value.improvement,
              mineralsCost,
              (int) planetInfoData.minerals);
          }
          else if(request.Request.Get().Value.improvement == Improvement.PROBE && planetInfoData.probes == planetInfoData.hangarLevel * 3)
          {
            response = String.Format("Not enough space in your hangar to build a probe");
          }
          else
          {
            // Adjust time for nanobot level
            timeRequired = mineralsCost / (2 * (1 + planetInfoData.nanobotLevel) * economySpeed);
            
            //Create new component update object
            PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
            planetInfoUpdate.SetBuildQueue(request.Request.Get().Value.improvement);
            planetInfoUpdate.SetBuildQueueTime(timeRequired);
            planetInfoUpdate.SetBuildMaterials(mineralsCost);
            connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);
            response = String.Format("Started building {0} on Planet {1} (it'll take {2} seconds)", request.Request.Get().Value.improvement, planetInfoData.name, timeRequired);
          }
        }
        
        var planetImprovementResponse = new PlanetImprovementResponse(response);
        var commandResponse = new PlanetImprovementResponder.Commands.PlanetImprovement.Response(planetImprovementResponse);
        connection.SendCommandResponse(request.RequestId, commandResponse);
      }
      else
      {
        logMessage = String.Format("No planet found for planetId {0}", request.Request.Get().Value.planetId);
        throw new SystemException(logMessage);
      }
    }
  }
}
