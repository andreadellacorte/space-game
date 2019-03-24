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
            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
              if(op.Authority == Authority.Authoritative)
              {
                  entity.hasAuthority = true;
              }
              else if (op.Authority == Authority.NotAuthoritative)
              {
                  entity.hasAuthority = false;
              }
              else if (op.Authority == Authority.AuthorityLossImminent)
              {
                  entity.hasAuthority = false;
              }
            }
          });
          
          dispatcher.OnAddComponent<PlanetInfo>(op =>
          {
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
            ViewEntity entity;
            if (EntityView.TryGetValue(op.EntityId, out entity))
            {
                var update = op.Update.Get();
                entity.entity.Update<PlanetInfo>(update);
            }
          });
          
          dispatcher.OnAddEntity(op =>
          {
            // AddEntity will always be followed by OnAddComponent
            ViewEntity newEntity = new ViewEntity();
            newEntity.hasAuthority = true;
            newEntity.entity = new Entity();
            ViewEntity oldEntity;
            if(!EntityView.TryGetValue(op.EntityId, out oldEntity))
            {
              EntityView.Add(op.EntityId, newEntity);
            }
          });
          
          dispatcher.OnRemoveEntity(op =>
          {
            EntityView.Remove(op.EntityId);
          });
          
          var entityIdReservationRequestId = default(RequestId<ReserveEntityIdsRequest>);
          var entityCreationRequestId = default(RequestId<CreateEntityRequest>);
            
          dispatcher.OnReserveEntityIdsResponse(op =>
          {
            if (op.RequestId == entityIdReservationRequestId && op.StatusCode == StatusCode.Success)
            {
              var entity = new Entity();
              // Empty ACL - should be customised.
              entity.Add(new Improbable.EntityAcl.Data(
                new Improbable.WorkerRequirementSet(new Improbable.Collections.List<Improbable.WorkerAttributeSet>()),
                new Improbable.Collections.Map<uint, Improbable.WorkerRequirementSet>()));
              // Needed for the entity to be persisted in snapshots.
              entity.Add(new Improbable.Persistence.Data());
              entity.Add(new Improbable.Metadata.Data(playerType));
              entity.Add(new Improbable.Position.Data(new Improbable.Coordinates(0, 0, 0)));
              entityCreationRequestId = connection.SendCreateEntityRequest(entity, op.FirstEntityId, timeoutMillis);
            }
          });
          
          dispatcher.OnCreateEntityResponse(op =>
          {
            if (op.RequestId == entityCreationRequestId && op.StatusCode == StatusCode.Success)
            {
              Console.WriteLine("Success!");
              
              foreach (KeyValuePair<EntityId, ViewEntity> pair in EntityView)
              {
                Option<IComponentData<PlanetInfo>> option;
                IComponentData<PlanetInfo> planetInfo;
                PlanetInfo.Data planetInfoData;
                PlanetInfoData pid;

                option = pair.Value.entity.Get<PlanetInfo>();
                planetInfo = option.Value;
                planetInfoData = planetInfo.Get();
                pid = planetInfoData.Value;
                
                if(pid.player.Id == 0)
                {
                  var id = pair.Key;
                  
                  //Create new component update object
                  PlanetInfo.Update piu = new PlanetInfo.Update();
                  
                  piu.SetPlayer(new EntityId(6));
                  
                  //Send the updates
                  connection.SendComponentUpdate<PlanetInfo>(id, piu);
                  
                  break;
                }
              }
            }
            
            Console.WriteLine("Failed for some reason");
          });

          dispatcher.OnCommandRequest<AssignPlanetResponder.Commands.AssignPlanet>(request =>
          {
              connection.SendLogMessage(LogLevel.Info, LoggerName, "Received AssignPlanet command");
              
              entityIdReservationRequestId = connection.SendReserveEntityIdsRequest(1, timeoutMillis);
              
          //    var greeting = hellos[random.Next(hellos.Length)];
          //     var pingResponse = new Pong(WorkerType, String.Format("{0}, World!", greeting));
          //     var commandResponse = new PingResponder.Commands.Ping.Response(pingResponse);
          //     connection.SendCommandResponse(request.RequestId, commandResponse);
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
  }
}
