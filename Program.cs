using Microsoft.AspNetCore.Mvc;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

var vectorizerSession = new InferenceSession("vectorizer.onnx");
var catboostSession = new InferenceSession("catboost_model.onnx");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

int PredictEmail(string text)
{
    var tensor = new DenseTensor<string>(new[] { 1, 1 });
    tensor[0, 0] = text;

    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("input", tensor)
    };

    using var vectorizerResults = vectorizerSession.Run(inputs);
    var vector = vectorizerResults.First().AsTensor<float>();

    var catboostInputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("features", vector)
    };

    using var catboostResults = catboostSession.Run(catboostInputs);

    var labelOutput = catboostResults.First(v => v.Name == "label");
    var prediction = labelOutput.AsTensor<long>().GetValue(0);

    return (int)prediction;
}

app.MapPost("/submit", ([FromForm] EmailData email) =>
{
    if (string.IsNullOrWhiteSpace(email.subject) || string.IsNullOrWhiteSpace(email.body))
    {
        return Results.BadRequest(new { message = "Enter valid Email subject and body" });
    }

    int result = PredictEmail(email.body);

    if (result == 1)
    {
        return Results.Ok(new { message = "Email is safe" });
    }

    return Results.Ok(new { message = "Email is unsafe" });
})
.DisableAntiforgery();

app.Run();