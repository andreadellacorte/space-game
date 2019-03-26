using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Improbable.SpatialOS.ServiceAccount.V1Alpha1;

namespace ServiceAccountMaintenance
{
  internal class Program
  {
    /// <summary>
    ///   The SpatialOS project name.
    /// </summary>
    private const string ProjectName = "beta_batman_crazy_339";

    /// <summary>
    ///   The name given to service accounts created during setup.
    /// </summary>
    private const string ServiceAccountName = "spacegame";

    private const int NumberOfServiceAccountsToCreate = 1;

    private static readonly ServiceAccountServiceClient ServiceAccountServiceClient =
      ServiceAccountServiceClient.Create();

    private static List<long> ServiceAccountIds;

    /// <summary>
    ///   Creates a few service accounts in your project.
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
      // Remove this line to delete all existing services in the project
      // DeleteAll(ProjectName);

      ServiceAccountIds = new List<long>();
      var permProject = new Permission
      {
        Parts = {new RepeatedField<string> {"prj", ProjectName, "*"}},
        Verbs =
        {
          new RepeatedField<Permission.Types.Verb>
          {
            Permission.Types.Verb.Read,
            Permission.Types.Verb.Write
          }
        }
      };

      var permServices = new Permission
      {
        Parts = {new RepeatedField<string> {"srv", "*"}},
        Verbs =
        {
          new RepeatedField<Permission.Types.Verb>
          {
            Permission.Types.Verb.Read
          }
        }
      };

      Console.WriteLine("Setting up for the scenario by creating new service accounts...");
      for (var i = 0; i < NumberOfServiceAccountsToCreate; i++)
      {
        var resp = ServiceAccountServiceClient.CreateServiceAccount(new CreateServiceAccountRequest
        {
            Name = ServiceAccountName,
            ProjectName = ProjectName,
            Permissions = {new RepeatedField<Permission> {permProject, permServices}},
            Lifetime = Duration.FromTimeSpan(new TimeSpan(3650, 0, 0, 0))
        });

        var daysUntilExpiry = Math.Floor((resp.ExpirationTime.ToDateTime() - DateTime.UtcNow).TotalDays);

        Console.WriteLine($"Service account '{resp.Name}' with id '{resp.Id}' will expire in {daysUntilExpiry} day(s)");
        Console.WriteLine($"Token for service account '{resp.Token}'");
      }
    }

    /// <summary>
    ///   This deletes all service accounts in the project.
    /// </summary>
    private static void DeleteAll(string projectName)
    {
      var serviceAccounts = ServiceAccountServiceClient.ListServiceAccounts(new ListServiceAccountsRequest
      {
        ProjectName = projectName
      });

      Console.WriteLine("Deleting up all service accounts found for the project:");
      foreach (var serviceAccount in serviceAccounts)
        ServiceAccountServiceClient.DeleteServiceAccount(new DeleteServiceAccountRequest
        {
          Id = serviceAccount.Id
        });
    }
  }
}