using Application.DTOs;
using AutoMapper;
using Domain.Models;

namespace Application.Mapping
{
    public class ValidationServiceProfile : Profile
    {
        public ValidationServiceProfile()
        {
            // DTO ? Domain
            CreateMap<VerifyCredentialResultDto, VerificationLog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Generate in constructor
                .ForMember(dest => dest.VcJwtHash, opt => opt.Ignore()) // Set separately
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
                .ForMember(dest => dest.FailureReason, opt => opt.MapFrom(src => src.FailureReason))
                .ForMember(dest => dest.VerifiedAt, opt => opt.MapFrom(src => src.VerifiedAt));
        }
    }
}