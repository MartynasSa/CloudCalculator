using Application.Models.Dtos;
using Application.Services.Calculator;

namespace Application.Facade;

public interface ICalculatorFacade
{
    Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest templateDto, CancellationToken ct = default);
}

public class CalculatorFacade(ICalculatorService calculatorService) : ICalculatorFacade
{
    public async Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest template, CancellationToken ct = default)
    {
        var result = await calculatorService.CalculateCostComparisonsAsync(template, ct);
        return result;
    }
}