using Api.Contracts;
using AutoMapper;
using Application.DTOs;
using Domain.Models;

namespace Api.Mapping
{
    public class okenServiceAPIProfile : Profile
    {
        public okenServiceAPIProfile()
        {
            // API → DTO
            CreateMap<IssueTokenRequestContract, IssueTokenDto>();

            // DTO → API
            CreateMap<IssuedTokenDto, IssueTokenResponseContract>();

          
        }
    }
}
