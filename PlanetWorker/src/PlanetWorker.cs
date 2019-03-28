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
    private const uint timeoutMillis = 500u;
    private const string WorkerType = "PlanetWorker";
    private const string LoggerName = "PlanetWorker.cs";
    private const int ErrorExitStatus = 1;
    private const uint GetOpListTimeoutInMilliseconds = 0;
    private const string playerType = "Player";
    private const int SecondsPerFrame = 2;
    
    private class ViewEntity
    {
        public bool hasAuthority;
        public Entity entity;
    }

    private static Dictionary<EntityId, ViewEntity> EntityView = new Dictionary<EntityId, ViewEntity>();
    
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
            //string logMessage;

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
              if(op.Authority == Authority.Authoritative)
              {
                //logMessage = String.Format("Gained authority over entityId {0}", op.EntityId);
                //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = true;
              }
              else if (op.Authority == Authority.NotAuthoritative)
              {
                //logMessage = String.Format("Lost authority over entityId {0}", op.EntityId);
                //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = false;
              }
              else if (op.Authority == Authority.AuthorityLossImminent)
              {
                //logMessage = String.Format("Lost authority over entityId {0}", op.EntityId);
                //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = false;
              }
            }
          });
          
          dispatcher.OnAddComponent<PlanetInfo>(op =>
          {
            //var logMessage = String.Format("Adding PlanetInfo Component for entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
                entity.entity.Add<PlanetInfo>(op.Data);
            }
            else
            {
                ViewEntity newEntity = new ViewEntity();
                EntityView.Add(op.EntityId, newEntity);
                newEntity.entity.Add<PlanetInfo>(op.Data);
            }
          });
          
          dispatcher.OnComponentUpdate<PlanetInfo>(op=>
          {
            //var logMessage = String.Format("Updating PlanetInfo Component for entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
                var update = op.Update.Get();
                entity.entity.Update<PlanetInfo>(update);
            }
          });
          
          dispatcher.OnAddEntity(op =>
          {
            //var logMessage = String.Format("Adding entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            // AddEntity will always be followed by OnAddComponent
            ViewEntity newEntity = new ViewEntity();
            newEntity.hasAuthority = false;
            newEntity.entity = new Entity();
            EntityView.Add(op.EntityId, newEntity);
          });
          
          dispatcher.OnRemoveEntity(op =>
          {
            //var logMessage = String.Format("Removing entityId {0}", op.EntityId);
            //connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
            EntityView.Remove(op.EntityId);
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
            
            foreach (var pair in EntityView)
            {
              if(isPlanet(pair.Value.entity) && pair.Value.hasAuthority) //Only do this update if this worker has write access to the entity
              {
                EntityId planetId = pair.Key;
                Entity entity = pair.Value.entity;
                
                PlanetInfoData planetInfoData = entity.Get<PlanetInfo>().Value.Get().Value;
                
                if(planetInfoData.playerId == "") // Don't do this update if the planet is uninhabited
                {
                  continue;
                }
                
                // Create new component update object
                PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
                                                
                if(planetInfoData.buildQueue == Improvement.MINE){
                  if(planetInfoData.buildQueueTime - 2 <= 0)
                  {
                    //Create new component update object
                    planetInfoUpdate.SetMineLevel(planetInfoData.mineLevel+1);
                    planetInfoUpdate.SetBuildQueue(Improvement.NONE);
                    planetInfoUpdate.SetBuildQueueTime(0);
                  }
                  else
                  {
                    planetInfoUpdate.SetBuildQueueTime(planetInfoData.buildQueueTime - 2);
                  }
                }
                
                planetInfoUpdate.SetMinerals(planetInfoData.minerals + planetInfoData.mineLevel);
                
                connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);
              }
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
      
      var planetAssigned = false;
        
      foreach (KeyValuePair<EntityId, ViewEntity> pair in EntityView)
      {
        if(!pair.Value.hasAuthority || !isPlanet(pair.Value.entity))
        {
          logMessage = String.Format("Skipping entity with entityId {0}", pair.Key);
          connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
          continue;
        }
        
        logMessage = String.Format("Picked planet with entityId {0} because I have authority", pair.Key);
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
        
        logMessage = String.Format("Entity.PlanetInfo: {0}", pair.Value.entity.Get<PlanetInfo>());
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
        
        PlanetInfoData planetInfoData = pair.Value.entity.Get<PlanetInfo>().Value.Get().Value;
        
        logMessage = String.Format("Entity {0} has playerId {1}", pair.Key, planetInfoData.playerId);
        connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
        
        if(planetInfoData.playerId == "")
        {
          var planetId = pair.Key;
          var planetName = planetInfoData.name;
          var playerId = request.Request.Get().Value.playerId;

          //Create new component update object
          PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
          planetInfoUpdate.SetPlayerId(playerId);
          connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);

          // Send the assigned planet to the client
          var assignPlanetResponse = new AssignPlanetResponse(planetId, planetName);
          var commandResponse = new AssignPlanetResponder.Commands.AssignPlanet.Response(assignPlanetResponse);
          connection.SendCommandResponse(request.RequestId, commandResponse);

          planetAssigned = true;

          break;
        }
      }
      
      if(!planetAssigned)
      {
        logMessage = String.Format("No planets available for player {0}", request.Request.Get().Value.playerId);
        throw new SystemException(logMessage);
      }
    }
      
    private static void HandlePlanetInfoRequest(CommandRequestOp<PlanetInfoResponder.Commands.PlanetInfo> request, Connection connection)
    {
      var logMessage = String.Format("Received PlanetInfo Command for EntityId {0}", request.Request.Get().Value.planetId);
      connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
      
      ViewEntity viewEntity;
      if (EntityView.TryGetValue(request.Request.Get().Value.planetId, out viewEntity))
      {
        PlanetInfoData planetInfoData = viewEntity.entity.Get<PlanetInfo>().Value.Get().Value;
        var planetInfoResponse = new PlanetInfoResponse(planetInfoData.name,
          planetInfoData.mineLevel, planetInfoData.minerals,
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
      
      ViewEntity viewEntity;
      if (EntityView.TryGetValue(request.Request.Get().Value.planetId, out viewEntity))
      {
        var planetId = request.Request.Get().Value.planetId;
        PlanetInfoData planetInfoData = viewEntity.entity.Get<PlanetInfo>().Value.Get().Value;
        
        string response;
        
        if(planetInfoData.buildQueue != Improvement.NONE)
        {
          response = String.Format("Build queue is already full; currently building: {0}", planetInfoData.buildQueue);
        }
        else if(request.Request.Get().Value.improvement == Improvement.MINE){
          //Create new component update object
          PlanetInfo.Update planetInfoUpdate = new PlanetInfo.Update();
          planetInfoUpdate.SetBuildQueue(request.Request.Get().Value.improvement);
          planetInfoUpdate.SetBuildQueueTime((int) planetInfoData.mineLevel*12);
          connection.SendComponentUpdate<PlanetInfo>(planetId, planetInfoUpdate);
          response = String.Format("Started building {0} on Planet {1}", request.Request.Get().Value.improvement, planetInfoData.name);
        }
        else
        {
          throw new SystemException("Unknown improvement type");
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
