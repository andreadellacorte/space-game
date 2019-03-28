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
    private const uint GetOpListTimeoutInMilliseconds = 100;
    private const string playerType = "Player";
    
    private class ViewEntity
    {
        public bool hasAuthority;
        public bool isPlanet;
        public Entity entity;
    }

    private static Dictionary<EntityId, ViewEntity> EntityView = new Dictionary<EntityId, ViewEntity>();

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
            string logMessage;

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
              if(op.Authority == Authority.Authoritative)
              {
                logMessage = String.Format("Gained authority over entityId {0}", op.EntityId);
                connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = true;
              }
              else if (op.Authority == Authority.NotAuthoritative)
              {
                logMessage = String.Format("Lost authority over entityId {0}", op.EntityId);
                connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = false;
              }
              else if (op.Authority == Authority.AuthorityLossImminent)
              {
                logMessage = String.Format("Lost authority over entityId {0}", op.EntityId);
                connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
                entity.hasAuthority = false;
              }
            }
          });
          
          dispatcher.OnAddComponent<PlanetInfo>(op =>
          {
            var logMessage = String.Format("Adding PlanetInfo Component for entityId {0}", op.EntityId);
            connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
                entity.entity.Add<PlanetInfo>(op.Data);
                entity.isPlanet = true;
            }
            else
            {
                ViewEntity newEntity = new ViewEntity();
                EntityView.Add(op.EntityId, newEntity);
                newEntity.entity.Add<PlanetInfo>(op.Data);
                newEntity.isPlanet = true;
            }
          });
          
          dispatcher.OnComponentUpdate<PlanetInfo>(op=>
          {
            var logMessage = String.Format("Updating PlanetInfo Component for entityId {0}", op.EntityId);
            connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
                var update = op.Update.Get();
                entity.entity.Update<PlanetInfo>(update);
            }
          });
          
          dispatcher.OnAddEntity(op =>
          {
            var logMessage = String.Format("Adding entityId {0}", op.EntityId);
            connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);

            // AddEntity will always be followed by OnAddComponent
            ViewEntity newEntity = new ViewEntity();
            newEntity.hasAuthority = false;
            newEntity.entity = new Entity();
            EntityView.Add(op.EntityId, newEntity);
          });
          
          dispatcher.OnRemoveEntity(op =>
          {
            var logMessage = String.Format("Removing entityId {0}", op.EntityId);
            connection.SendLogMessage(LogLevel.Info, LoggerName, logMessage);
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

          connection.SendLogMessage(LogLevel.Info, LoggerName,
            "Successfully connected using TCP and the Receptionist");

          while (isConnected)
          {
            using (var opList = connection.GetOpList(GetOpListTimeoutInMilliseconds))
            {
              dispatcher.Process(opList);
            }
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
        if(!pair.Value.hasAuthority || !pair.Value.isPlanet)
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
          
          // Send the updates
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
      
      ViewEntity entity;
      if (EntityView.TryGetValue(request.Request.Get().Value.planetId, out entity))
      {
        PlanetInfoData planetInfoData = entity.entity.Get<PlanetInfo>().Value.Get().Value;
        var planetInfoResponse = new PlanetInfoResponse(planetInfoData.name, planetInfoData.minerals);
        var commandResponse = new PlanetInfoResponder.Commands.PlanetInfo.Response(planetInfoResponse);
        connection.SendCommandResponse(request.RequestId, commandResponse);
      }
      else
      {
        logMessage = String.Format("No planets available for planetId {0}", request.Request.Get().Value.planetId);
        throw new SystemException(logMessage);
      }
    }
  }
}
