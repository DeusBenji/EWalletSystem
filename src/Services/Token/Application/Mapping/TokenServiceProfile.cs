using Application.DTOs;
using AutoMapper;
using Domain.Models;

namespace Application.Mapping
{
    public class TokenServiceProfile : Profile
    {
        public TokenServiceProfile()
        {
           
            // Domain → DTO (hvis nødvendigt senere)
            CreateMap<AgeAttestation, IssuedTokenDto>();

            // DTO → Domain (hvis nødvendigt – normalt ikke)
        }
    }
}
