// Copyright (c) Improbable Worlds Ltd, All Rights Reserved

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Timers;

using Improbable;
using Improbable.Worker;
using Improbable.Collections;

using Demo.Galaxies;

namespace Demo
{
  class SnapshotGenerator
  {
    private const int WorldDimension = 2200;
    private const int AuthorityMarketSpacing = 200;
    private static readonly int[] WorkerLocations = Enumerable.Range(-WorldDimension / (2 * AuthorityMarketSpacing), WorldDimension / AuthorityMarketSpacing).Select(x => x * AuthorityMarketSpacing).ToArray();

    private const int ErrorExitStatus = 1;
    private const string LoggerName = "SnapshotGenerator.cs";

    private static readonly string[] WorkerLayers = {"planets"};
    private static readonly Random random = new Random();
    
    static int Main(string[] arguments)
    {
      Action printUsage = () =>
      {
        Console.WriteLine("Usage: SnapshotGenerator <snapshotfile>");
        Console.WriteLine("Generates a snapshot.");
        Console.WriteLine("    <snapshotfile> will generate a snapshot and exit.");
      };

      if (arguments.Length != 1)
      {
        printUsage();
        return ErrorExitStatus;
      }
      
      Console.WriteLine(WorkerLocations);

      GenerateSnapshot(arguments[0], WorkerLayers, WorkerLocations);
      
      return 0;
    }
    
    public static void GenerateSnapshot(string snapshotPath, string[] workerLayers, int[] workerLocations)
    {
      Console.WriteLine(String.Format("Generating snapshot file {0}", snapshotPath));
      using (var snapshotOutput = new SnapshotOutputStream(snapshotPath))
      {
        var entityId = 1;
        Entity entity;
        Option<String> error;

        for (var l = 0; l < workerLayers.Length; l++)
        {
          for (var x = 0; x < workerLocations.Length; x++)
          {
            for (var z = 0; z < workerLocations.Length; z++)
            {
              entity = createAuthorityMarker(workerLayers[l], workerLocations[x], workerLocations[z]);
              error = snapshotOutput.WriteEntity(new EntityId(entityId), entity);
              if (error.HasValue)
              {
                  throw new System.SystemException("error saving: " + error.Value);
              }

              entityId++;
            }
          }
        }
        
        var galaxy = Galaxy.Generate(new Spiral(), new Random());
        
        // Create one planet
        foreach (var star in galaxy.Stars)
        {
          entity = createPlanet(star);
          error = snapshotOutput.WriteEntity(new EntityId(entityId), entity);
          if (error.HasValue)
          {
              throw new System.SystemException("error saving: " + error.Value);
          }

          entityId++;
        }
      }
    }
    
    private static Entity createPlanet(Star star)
    {
      const string entityType = "Planet";
      const string empty_player_name = "";
      const int default_mine_level = 1;
      const int default_build_queue_time = 0;
      const int default_build_materials = 0;
      const int default_deposit_level = 1;
      const int default_probes = 0;
      const int default_hangar_level = 1;
      const int default_nanobots_level = 0;
      
      string random_password = random.Next().ToString("X");

      int random_minerals = 100;
      
      // Defines worker attribute requirements for workers that can read a component.
      // workers with an attribute of "client" OR workerType will have read access
      var readRequirementSet = new WorkerRequirementSet(
        new Improbable.Collections.List<WorkerAttributeSet>
        {
            new WorkerAttributeSet(new Improbable.Collections.List<string> {"planets"}),
        });

      // Defines worker attribute requirements for workers that can write to a component.
      // workers with an attribute of workerType will have write access
      var workerWriteRequirementSet = new WorkerRequirementSet(
        new Improbable.Collections.List<WorkerAttributeSet>
        {
            new WorkerAttributeSet(new Improbable.Collections.List<string> {"planets"}),
        });
      
      var writeAcl = new Improbable.Collections.Map<uint, WorkerRequirementSet>
      {
        {EntityAcl.ComponentId, workerWriteRequirementSet},
        {Position.ComponentId, workerWriteRequirementSet},
        {PlanetInfo.ComponentId, workerWriteRequirementSet},
        {PlanetInfoResponder.ComponentId, workerWriteRequirementSet},
        {PlanetImprovementResponder.ComponentId, workerWriteRequirementSet}
      };
      
      var entity = new Entity();

      entity.Add(new EntityAcl.Data(readRequirementSet, writeAcl));
      // Needed for the entity to be persisted in snapshots.
      entity.Add(new Persistence.Data());
      entity.Add(new Metadata.Data(entityType));
      entity.Add(new Position.Data(new Coordinates(star.Position.X, star.Position.Y, star.Position.Z)));
      entity.Add(new PlanetInfo.Data(star.Name,
        empty_player_name,
        default_mine_level, random_minerals, default_deposit_level,
        default_probes, default_hangar_level,
        default_nanobots_level,
        Improvement.EMPTY, default_build_queue_time, default_build_materials,
        random_password));
      entity.Add(new PlanetInfoResponder.Data());
      entity.Add(new PlanetImprovementResponder.Data());
      
      return entity;
    }

    private static Entity createAuthorityMarker(string workerType, int x, int z)
    {
      var entity = new Entity();
      const string entityType = "AuthorityMarker";

      // Defines worker attribute requirements for workers that can read a component.
      // workers with an attribute of "client" OR workerType will have read access
      var readRequirementSet = new WorkerRequirementSet(
        new Improbable.Collections.List<WorkerAttributeSet>
        {
            new WorkerAttributeSet(new Improbable.Collections.List<string> {workerType}),
            new WorkerAttributeSet(new Improbable.Collections.List<string> {"client"}),
        });

      // Defines worker attribute requirements for workers that can write to a component.
      // workers with an attribute of workerType will have write access
      var workerWriteRequirementSet = new WorkerRequirementSet(
        new Improbable.Collections.List<WorkerAttributeSet>
        {
          new WorkerAttributeSet(new Improbable.Collections.List<string> {workerType}),
        });
      
      var writeAcl = new Improbable.Collections.Map<uint, WorkerRequirementSet>
      {
        {EntityAcl.ComponentId, workerWriteRequirementSet},
        {Position.ComponentId, workerWriteRequirementSet},
        {AssignPlanetResponder.ComponentId, workerWriteRequirementSet}
      };

      entity.Add(new EntityAcl.Data(readRequirementSet, writeAcl));
      // Needed for the entity to be persisted in snapshots.
      entity.Add(new Persistence.Data());
      entity.Add(new Metadata.Data(entityType));
      entity.Add(new Position.Data(new Coordinates(x, 0, z)));
      entity.Add(new AssignPlanetResponder.Data());
      return entity;
    }
  }
}