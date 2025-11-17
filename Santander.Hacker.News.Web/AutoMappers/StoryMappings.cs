using AutoMapper;
using Santander.Hacker.News.Domains;
using Santander.Hacker.News.Web.ViewModels;

namespace Santander.Hacker.News.Web.AutoMappers
{
    public static class StoryMappings
    {
        public static void Register(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<Story, StoryViewModel>()
                .ForMember(dest => dest.Uri, opt => opt.MapFrom(src => src.Url))
                .ForMember(dest => dest.PostedBy, opt => opt.MapFrom(src => src.By))
                .ForMember(dest => dest.Time, opt => opt.MapFrom(src =>
                    src.Time.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(src.Time.Value).ToString("yyyy-MM-ddTHH:mm:sszzz")
                        : null))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Descendants ?? 0))
                // Title maps by convention
                ;
        }
    }
}
