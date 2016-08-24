using System;
using AutoMapper;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JsonApiDotNetCore.Extensions
{
  public static class IServiceCollectionExtensions
  {
    public static void AddJsonApi(this IServiceCollection services, Action<IJsonApiModelConfiguration> configurationAction)
    {
      var config = new JsonApiModelConfiguration();
      configurationAction.Invoke(config);

      if (config.ResourceMaps == null)
      {
        config.ResourceMaps = new MapperConfiguration(cfg => {}).CreateMapper();
      }

      services.AddSingleton(_ => new JsonApiService(config));
    }
  }
}