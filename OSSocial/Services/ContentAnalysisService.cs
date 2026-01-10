using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSSocial.Services
{
    // =====================================================================
    // SERVICIU DE ANALIZĂ SENTIMENT FOLOSIND GOOGLE AI (GEMINI)
    // =====================================================================
    //
    // Acest fișier conține implementarea serviciului de analiză sentiment
    // folosind Google Generative AI (Gemini) în loc de OpenAI.
    //
    // PAȘI PENTRU A SCHIMBA DE LA OPENAI LA GOOGLE AI:
    // =====================================================================
    //
    // 1. În fișierul appsettings.json, adăugați configurația pentru Google AI:
    //
    //    "GoogleAI": {
    //        "ApiKey": "CHEIA_TA_API_GOOGLE"
    //    }
    //
    // 2. În fișierul Program.cs, schimbați înregistrarea serviciului:
    //
    //    // ÎNAINTE (OpenAI):
    //    // builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
    //
    //    // DUPĂ (Google AI):
    //    builder.Services.AddScoped<ISentimentAnalysisService, GoogleSentimentAnalysisService>();
    //
    // 3. Asigurați-vă că aveți o cheie API validă de la Google AI Studio:
    //    https://aistudio.google.com/app/apikey
    //
    // =====================================================================

    /// <summary>
    /// Implementarea serviciului de analiză sentiment folosind Google AI (Gemini)
    /// Această clasă implementează aceeași interfață ISentimentAnalysisService
    /// pentru a permite schimbarea ușoară între provideri (OpenAI/Google)
    /// </summary>
    ///
    ///

    public class ContentResult
    {
        // daca e acceptat sau nu mesajul
        public bool IsAccepted { get; set; }

        // daca nu e acceptat, motivul
        public string? Reason { get; set; }
        
        public bool Success { get; set; }
        
        public string? ErrorMessage { get; set; }
    }
    
    public interface IContentAnalysisService
    {
        Task<ContentResult> AnalyzeContentAsync(string text);
    }
    
    public class ContentAnalysisService : IContentAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<ContentAnalysisService> _logger;

        // URL-ul de bază pentru API-ul Google Generative AI
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        // Modelul folosit - gemini-2.5-flash-lite
        private const string ModelName = "gemini-2.5-flash-lite";

        public ContentAnalysisService(IConfiguration configuration, ILogger<ContentAnalysisService> logger)
        {
            _httpClient = new HttpClient();

            // Citim cheia API din configurație
            // Asigurați-vă că ați adăugat "GoogleAI:ApiKey" în appsettings.json
            _apiKey = configuration["GoogleAI:ApiKey"]
                ?? throw new ArgumentNullException("GoogleAI:ApiKey nu este configurat în appsettings.json");

            _logger = logger;

            // Configurare HttpClient pentru Google AI API
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Analizează continutul unui text folosind Google AI (Gemini)
        /// </summary>
        /// <param name="text">Textul de analizat</param>
        /// <returns>Rezultatul analizei de sentiment</returns>
        public async Task<ContentResult> AnalyzeContentAsync(string text)
        {
            try
            {
                // Construim prompt-ul pentru analiza de sentiment
                // Același format ca la OpenAI pentru consistență
                var prompt = $@"You are a content analysis assistant. Analyze the content of the given text and respond ONLY with a JSON object in this exact format:
                {{""IsAccepted"": true, ""Reason"": ""explanation string""}}

                Rules:
                - IsAccepted must be exactly one of: true, false
                - If IsAccepted is false, provide a Reason for rejection in the explanation string
                - the explanation string has to be explicit and shorter then 450 characters
                - If IsAccepted is true, Reason can be an empty string
                - IsAccepted must be a JSON boolean: true or false (NOT a string ""true"") 
                - SHORT CONTENT RULE: Short text, single words, or neutral statements (e.g., """"Hi"""", """"Update"""", """"Cool"""") are ACCEPTABLE (IsAccepted: true) unless they are explicitly vulgar/offensive. 
                - EMPTY CONTENT RULE: text can be empty
                - Do not include any other text, only the JSON object
               

                Analyze the content of this comment: ""{text}""";

                // Construim request-ul pentru Google AI API
                // Structura este diferită față de OpenAI - folosim "contents" și "parts"
                var requestBody = new GoogleAiRequest
                {
                    Contents = new List<GoogleAiContent>
                    {
                        new GoogleAiContent
                        {
                            Parts = new List<GoogleAiPart>
                            {
                                new GoogleAiPart { Text = prompt }
                            }
                        }
                    },
                    // Configurări pentru generare - temperature scăzută pentru rezultate consistente
                    GenerationConfig = new GoogleAiGenerationConfig
                    {
                        Temperature = 0.1,
                        MaxOutputTokens = 100
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Construim URL-ul complet cu cheia API ca parametru
                // Google AI folosește X-goog-api-key sau parametru în URL
                var requestUrl = $"{BaseUrl}{ModelName}:generateContent?key={_apiKey}";

                _logger.LogInformation("Trimitem cererea de analiză Content către Google AI API");

                // Trimitem request-ul către Google AI API
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google AI API Error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new ContentResult
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode}"
                    };
                }

                // Parsăm răspunsul de la Google AI
                var googleResponse = JsonSerializer.Deserialize<GoogleAiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Extragem textul din răspuns
                // Structura: candidates[0].content.parts[0].text
                var assistantMessage = googleResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(assistantMessage))
                {
                    return new ContentResult
                    {
                        Success = false,
                        ErrorMessage = "Empty response from API"
                    };
                }

                _logger.LogInformation("Empty response from Google AI: {Response}", assistantMessage);

                // Curățăm răspunsul de eventuale caractere markdown (```json ... ```)
                var cleanedResponse = CleanJsonResponse(assistantMessage);

                // Parsăm JSON-ul din răspunsul asistentului
                var ContentData = JsonSerializer.Deserialize<ContentResult>(cleanedResponse);

                if (ContentData == null)
                {
                    return new ContentResult
                    {
                        Success = false,
                        ErrorMessage = "Answer could not be parsed"
                    };
                }

                // validare valori
                
                
                var isAccepted = ContentData.IsAccepted switch
                {
                    true => true,
                    false => false,
                };         
                
                var reason = ContentData.Reason;
                if (reason != null && reason.Length > 450)
                {
                    reason = reason.Substring(0, 450); 
                }
                
                return new ContentResult
                {
                    IsAccepted = isAccepted,
                    Reason = reason,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content could not be analysed");
                return new ContentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Curăță răspunsul JSON de eventuale caractere markdown
        /// Gemini poate returna răspunsul înconjurat de ```json ... ```
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();

            // Eliminăm blocurile de cod markdown dacă există
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }

            return cleaned.Trim();
        }
    }

    // =====================================================================
    // CLASE PENTRU SERIALIZAREA/DESERIALIZAREA RĂSPUNSURILOR GOOGLE AI
    // =====================================================================
    //
    // Structura request-ului Google AI:
    // {
    //   "contents": [
    //     {
    //       "parts": [
    //         { "text": "..." }
    //       ]
    //     }
    //   ]
    // }
    //
    // Structura răspunsului Google AI:
    // {
    //   "candidates": [
    //     {
    //       "content": {
    //         "parts": [
    //           { "text": "..." }
    //         ]
    //       }
    //     }
    //   ]
    // }
    // =====================================================================

    /// <summary>
    /// Clasa pentru request-ul către Google AI
    /// </summary>
    public class GoogleAiRequest
    {
        [JsonPropertyName("contents")]
        public List<GoogleAiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GoogleAiGenerationConfig? GenerationConfig { get; set; }
    }

    /// <summary>
    /// Conținutul mesajului pentru Google AI
    /// </summary>
    public class GoogleAiContent
    {
        [JsonPropertyName("parts")]
        public List<GoogleAiPart> Parts { get; set; } = new();
    }

    /// <summary>
    /// O parte din conținut (text, imagine, etc.)
    /// </summary>
    public class GoogleAiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurări pentru generarea răspunsului
    /// </summary>
    public class GoogleAiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; } = 1024;
    }

    /// <summary>
    /// Răspunsul de la Google AI API
    /// </summary>
    public class GoogleAiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GoogleAiCandidate>? Candidates { get; set; }
    }

    /// <summary>
    /// Un candidat din răspuns (Google AI poate returna mai mulți candidați)
    /// </summary>
    public class GoogleAiCandidate
    {
        [JsonPropertyName("content")]
        public GoogleAiContent? Content { get; set; }
    }
}
