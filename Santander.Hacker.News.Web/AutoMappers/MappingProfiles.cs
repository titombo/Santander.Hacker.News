using AutoMapper;

namespace Santander.Hacker.News.Web.AutoMappers
{
    // Central registry that calls each entity's mapping registration.
    // Add new entity mapping registrations here so Program.cs needs only one call.
    public static class MappingProfiles
    {
        public static void Map(IMapperConfigurationExpression cfg)
        {
            // Per-entity mapping registrations
            StoryMappings.Register(cfg);

            // future: OtherEntityMappings.Register(cfg);
        }
    }
}
