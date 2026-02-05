using Api.Contracts;
using AutoMapper;
using Application.DTOs;

namespace Api.Mapping
{
    public class ApiContractMappingProfile : Profile
    {
        public ApiContractMappingProfile()
        {
            CreateMap<VerifyCredentialRequest, VerifyCredentialDto>();

            CreateMap<VerifyCredentialResultDto, VerifyCredentialResponse>()
                .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.IsValid));
        }
    }
}