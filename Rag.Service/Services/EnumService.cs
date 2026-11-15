using Rag.Service.Dtos;
using Rag.Service.Enums;
using Rag.Service.Services.Interfaces;
using Serilog;
using System.ComponentModel;
using System.Reflection;

namespace Rag.Service.Services
{
    public class EnumService : IEnumService
    {
        public List<EnumDropdownItem> CastLlmProviderEnumIntoList()
        {
            try
            {
                return CastEnumToDropdownList<LlmProviderEnum>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error casting LlmProviderEnum to dropdown list");
                throw;
            }
        }
        private List<EnumDropdownItem> CastEnumToDropdownList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Select(value => new EnumDropdownItem
                       {
                           Name = GetEnumDescription(value),
                           Id = Convert.ToInt32(value)
                       })
                       .ToList();
        }

        private string GetEnumDescription<TEnum>(TEnum value) where TEnum : Enum
        {
            var field = typeof(TEnum).GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }
    }
}
