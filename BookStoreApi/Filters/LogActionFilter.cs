using Microsoft.AspNetCore.Mvc.Filters;

namespace BookStoreApi.Filters;

public class LogActionFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Console.WriteLine($"[LOG] Starting: {context.ActionDescriptor.DisplayName}");
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        Console.WriteLine($"[LOG] Finished: {context.ActionDescriptor.DisplayName}");
    }
}