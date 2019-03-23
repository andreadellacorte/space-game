// Copyright (c) Improbable Worlds Ltd, All Rights Reserved

using System;
using System.Reflection;
using Improbable;
using Improbable.Worker;
using Improbable.Collections;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Demo
{
    public static class SnapshotGenerator
    {
        public static void GenerateSnapshot(string snapshotPath, string[] workers)
        {
            Console.WriteLine(String.Format("Generating snapshot file {0}", snapshotPath));
            using (var snapshotOutput = new SnapshotOutputStream(snapshotPath))
            {
                var entityId = 1;
                Entity entity;
                Option<String> error;

                for (var i = 0; i < workers.Length; i++)
                {
                    var workerType = workers[i];
                    entity = createAuthorityMarker(workerType);
                    error = snapshotOutput.WriteEntity(new EntityId(entityId), entity);
                    if (error.HasValue)
                    {
                        throw new System.SystemException("error saving: " + error.Value);
                    }

                    entityId++;
                }
                
                // Create one planet
                entity = createPlanet();
                error = snapshotOutput.WriteEntity(new EntityId(entityId), entity);
                if (error.HasValue)
                {
                    throw new System.SystemException("error saving: " + error.Value);
                }
            }
        }
        
        private static Entity createPlanet()
        {
            var entity = new Entity();
            const string entityType = "Planet";
            
            // Defines worker attribute requirements for workers that can read a component.
            // workers with an attribute of "client" OR workerType will have read access
            var readRequirementSet = new WorkerRequirementSet(
                new Improbable.Collections.List<WorkerAttributeSet>
                {
                    new WorkerAttributeSet(new Improbable.Collections.List<string> {"planets"}),
                    new WorkerAttributeSet(new Improbable.Collections.List<string> {"client"}),
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
                {PlanetInfo.ComponentId, workerWriteRequirementSet}
            };
            
            entity.Add(new EntityAcl.Data(readRequirementSet, writeAcl));
            // Needed for the entity to be persisted in snapshots.
            entity.Add(new Persistence.Data());
            entity.Add(new Metadata.Data(entityType));
            entity.Add(new Position.Data(new Coordinates(0, 0, 0)));
            entity.Add(new PlanetInfo.Data("Planet Name", 0));
            return entity;
        }

        private static Entity createAuthorityMarker(string workerType)
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
                {Position.ComponentId, workerWriteRequirementSet}
            };

            entity.Add(new EntityAcl.Data(readRequirementSet, writeAcl));
            // Needed for the entity to be persisted in snapshots.
            entity.Add(new Persistence.Data());
            entity.Add(new Metadata.Data(entityType));
            entity.Add(new Position.Data(new Coordinates(0, 0, 0)));
            return entity;
        }
    }
}
