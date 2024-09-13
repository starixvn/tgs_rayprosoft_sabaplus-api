using System.Globalization;
using AutoMapper;
using Newtonsoft.Json;
using SabaPlus.API.Models.EndGame;
using SabaPlus.API.Models.PlaceBet;
using SabaPlus.Repositories.Models;

namespace SabaPlus.API;

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		CreateMap<PlaceBetDataSet, PlaceBetCriteria>()
			.ForMember(dest => dest.ExternalGameId, opt => opt.MapFrom(src => src.GameId))
			.ForMember(dest => dest.ExtraInfo, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.OtherBetInfo)));
		CreateMap<EndGameDataSet, EndGameCriteria>()
			.ForMember(dest => dest.ExternalGameId, opt => opt.MapFrom(src => src.GameId))
			.ForMember(dest => dest.SettleDate, opt => opt.MapFrom(src => DateTime.Parse(src.SettleDate!, null, DateTimeStyles.RoundtripKind).ToUniversalTime().AddHours(4)))
			.ForMember(dest => dest.ExtraInfo, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.OtherBetInfo)));
	}
}