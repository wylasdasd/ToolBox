using ToolBox.Services.Ai;

namespace ToolBox.Web;

public static class AiChatApi
{
    public static void MapAiChatApi(this WebApplication app)
    {
        app.MapPost("/api/ai/complete", async (AiChatRequest request, IAiChatService chatService, CancellationToken cancellationToken) =>
        {
            try
            {
                var content = await chatService.CompleteAsync(request, cancellationToken);
                return Results.Json(new { content });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
            }
        }).DisableAntiforgery();
    }
}
