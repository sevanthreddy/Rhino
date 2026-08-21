using System.Collections.Immutable;
using System.Text.Json;
using Backend.Data;
using Backend.Migrations;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;

    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ApplicationDbContext applicationDbContext,IConfiguration configuration,ILogger<MessageService> logger)
    {
        _context = applicationDbContext;
        _configuration=configuration;
        _logger=logger;
    }
    public async Task<MessageDto> CreateMessageAsync(int senderid, string content, int receiverid)
    {
        try
        {
            string? embeddingJson = null;
            if ( _configuration.GetValue<bool>("enableEmbeddings"))
            {
                embeddingJson = await GenerateEmbeddingAsync(content);
            }
            var r = await _context.Message.AddAsync(new Message
            {
                Content = content,
                SenderId = senderid,
                ReceiverId = receiverid,
                SentAt = DateTime.UtcNow,
                Status = "Sent",
                Embedding = embeddingJson
            });
            await _context.SaveChangesAsync();
            return new MessageDto
            {
                MessageId = r.Entity.MessageId,
                SenderId = r.Entity.SenderId,
                ReceiverId = r.Entity.ReceiverId,
                Content = r.Entity.Content,
                SentAt = r.Entity.SentAt,
                Status = r.Entity.Status
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CREATE MESSAGE FAILED for sender {SenderId} and receiver {ReceiverId}", senderid, receiverid);
            return new MessageDto();
        }
    }

    public async Task<IEnumerable<ChatInfoDto>> GetAllPrevChatsForAUserAsync(int mainid)
    {
        try
        {
            var people = (await _context.Message.Include(m => m.Sender).Include(m => m.Receiver)
    .Where(m => m.SenderId == mainid || m.ReceiverId == mainid)
    .Select(m => m.SenderId == mainid
        ? new ChatInfoDto
        {
            userid = m.ReceiverId,
            Name = m.Receiver.Username,
            ProfilePicture = m.Receiver.ProfileImageURL

        }
        : new ChatInfoDto
        {
            userid = m.SenderId,
            Name = m.Sender.Username,
            ProfilePicture = m.Sender.ProfileImageURL

        })
    .ToListAsync())
    .GroupBy(x => x.userid)
    .Select(g => g.First())
    .ToList();

            return people;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET PREVIOUS CHATS FAILED for user {UserId}", mainid);
            return Enumerable.Empty<ChatInfoDto>();

        }
    }

    public async Task<IEnumerable<ChatInfoDto>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users.Select(x => new ChatInfoDto
            {
                Name = x.Username,
                userid = x.Id
            }).ToListAsync();
            return users;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET ALL USERS FAILED");
            return Enumerable.Empty<ChatInfoDto>();
        }
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesBetweenUsersAsync(int senderid, int receiverid, int? lastmessageid, int numberofmessages = 30)
    {
        try
        {
            _logger.LogInformation("Getting messages between users {SenderId} and {ReceiverId}; last message {LastMessageId}; requested count {Count}", senderid, receiverid, lastmessageid, numberofmessages);
            var query = _context.Message.Where(m =>
    (m.SenderId == senderid && m.ReceiverId == receiverid) ||
    (m.SenderId == receiverid && m.ReceiverId == senderid));

            if (lastmessageid.HasValue)
            {
                query = query.Where(m => m.MessageId < lastmessageid.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.MessageId)
                .Take(numberofmessages)
                .ToListAsync();

            messages.Reverse();

            return messages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                SentAt = m.SentAt,
                Status = m.Status
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET MESSAGES FAILED for sender {SenderId} and receiver {ReceiverId}", senderid, receiverid);
            return Enumerable.Empty<MessageDto>();
        }
    }

    public async Task<bool> UpdateStatusAsync(int messageid, string status)
    {
        try
        {
            var res = await _context.Message.Where(m => m.MessageId == messageid).OrderByDescending(m => m.SentAt)
    .FirstOrDefaultAsync();
            res.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UPDATE MESSAGE STATUS FAILED for message {MessageId}", messageid);
            return false;
        }

    }
    public async Task<IEnumerable<int>> UpdateStatusAllAsync(int senderid, int receiverid)
    {
        try
        {
            var messages = await _context.Message.Where(m => m.SenderId == receiverid && m.ReceiverId == senderid).ToListAsync();
            foreach (var message in messages)
            {
                message.Status = "Read";
            }
            await _context.SaveChangesAsync();
            return messages.Select(m => m.MessageId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UPDATE MESSAGE STATUSES FAILED for sender {SenderId} and receiver {ReceiverId}", senderid, receiverid);
            return Enumerable.Empty<int>();
        }

    }

    public async Task<IEnumerable<int>> GetUnreadMessagesAsync(int userid)
    {
        try
        {
            var listofsenders = await _context.Message
         .Where(m => m.ReceiverId == userid && m.Status != "Read")
         .Select(m => m.SenderId)
         .Distinct().ToListAsync();


            return listofsenders;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET UNREAD MESSAGES FAILED for user {UserId}", userid);
            return Enumerable.Empty<int>();
        }
    }

    public async Task<IEnumerable<SearchResultDto>> GetSearchResultsAsync(string searchTerm, int mainid)
    {
        try
        {
            Dictionary<int, double> scores = new();// Dictionary to hold the  scores for each message of the main user
                                                   // Search for messages in db  containing the search term and involving the main user
                                                   //all the messages involving the main user and containing the search term will get lexicalscore
            var results = await _context.Message.Where(m => m.Content.Contains(searchTerm) && (m.SenderId == mainid || m.ReceiverId == mainid))
                .Select(m => new SearchResultDto
                {
                    Userid = m.SenderId == mainid ? m.ReceiverId : m.SenderId,
                    Name = m.SenderId == mainid ? m.Receiver.Username : m.Sender.Username,
                    Content = m.Content,
                    Messageid = m.MessageId
                })
                .ToListAsync();
            for (int i = 0; i < results.Count; i++)
            {
                string[] messageWords = results[i].Content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int matchingWords = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(word => messageWords.Contains(word));
                double lexicalScore = (double)matchingWords / searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                scores[results[i].Messageid] = lexicalScore; // Assigning a lexical score to messages containing the search term
            }
            var embeddingJson = await GenerateEmbeddingAsync(searchTerm);
            var allMessagesOfMainUser = await _context.Message.Where(m => m.SenderId == mainid || m.ReceiverId == mainid).Include(m => m.Sender)
    .Include(m => m.Receiver).ToListAsync();
            var finalresults = new List<SearchResultDto>();
            for (int i = 0; i < allMessagesOfMainUser.Count; i++)
            {
                var string1 = JsonSerializer.Deserialize<List<float>>(embeddingJson)!;
                if (string.IsNullOrEmpty(allMessagesOfMainUser[i].Embedding))
                {
                    continue;
                }

                var string2 = JsonSerializer.Deserialize<List<float>>(
                    allMessagesOfMainUser[i].Embedding
                )!;
                if (string2 == null || string1 == null)
                {
                    continue;
                }
                var similarity = CosineSimilarity(string1, string2);
                double lexicalScore = scores.TryGetValue(allMessagesOfMainUser[i].MessageId, out var lexical) ? lexical : 0;

                double finalScore =
                    (lexicalScore * 0.3) +
                    (similarity * 0.7);

                scores[allMessagesOfMainUser[i].MessageId] = finalScore;

                if (scores[allMessagesOfMainUser[i].MessageId] > 0.1)
                {
                    finalresults.Add(new SearchResultDto
                    {
                        Userid = allMessagesOfMainUser[i].SenderId == mainid ? allMessagesOfMainUser[i].ReceiverId : allMessagesOfMainUser[i].SenderId,
                        Name = allMessagesOfMainUser[i].SenderId == mainid ? allMessagesOfMainUser[i].Receiver.Username : allMessagesOfMainUser[i].Sender.Username,
                        Content = allMessagesOfMainUser[i].Content,
                        Messageid = allMessagesOfMainUser[i].MessageId
                    });
                }

            }
            return finalresults.OrderByDescending(r => scores[r.Messageid]).Take(10).ToList();
            //have to send +2 and -2 messages of each message in the finalresults to llm for ranking as it can understand what we are asking?
            var relevantMessages = new Dictionary<int, List<Message>>();// Dictionary to hold the relevant messages
            for (int i = 0; i < finalresults.Count; i++)
            {
                var messageid = finalresults[i].Messageid;
                // Get the candidate message
                var candidate = await _context.Message
                    .FirstOrDefaultAsync(m => m.MessageId == messageid);

                if (candidate == null)
                    continue;

                // Find the other person in this conversation
                int otherUserId =
                    candidate.SenderId == mainid
                        ? candidate.ReceiverId
                        : candidate.SenderId;

                // Get 2 messages BEFORE the candidate
                var previousMessages = await _context.Message
                    .Where(m =>
                        (
                            (m.SenderId == mainid && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == mainid)
                        )
                        &&
                        m.SentAt < candidate.SentAt
                    )
                    .OrderByDescending(m => m.SentAt)
                    .Take(5)
                    .ToListAsync();

                // Get 2 messages AFTER the candidate
                var nextMessages = await _context.Message
                    .Where(m =>
                        (
                            (m.SenderId == mainid && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == mainid)
                        )
                        &&
                        m.SentAt > candidate.SentAt
                    )
                    .OrderBy(m => m.SentAt)
                    .Take(5)
                    .ToListAsync();

                // Put everything together
                var contextMessages = new List<Message>();

                // Previous messages were fetched newest-first,
                // so reverse them before adding.
                previousMessages.Reverse();

                contextMessages.AddRange(previousMessages);

                // Candidate itself
                contextMessages.Add(candidate);

                // Next messages are already oldest-first
                contextMessages.AddRange(nextMessages);

                // Store using candidate MessageId as the key
                relevantMessages[candidate.MessageId] = contextMessages;


            }



            var res2 = await GetReleventMessagesFromLLMAsync(relevantMessages, searchTerm, mainid);

            var messages = await _context.Message.Where(m => res2.Contains(m.MessageId)).Select(m => new SearchResultDto
            {
                Userid = m.SenderId == mainid ? m.ReceiverId : m.SenderId,
                Name = m.SenderId == mainid ? m.Receiver.Username : m.Sender.Username,
                Content = m.Content,
                Messageid = m.MessageId
            }).ToListAsync();
            var ultimateresults = res2
    .Select(id => messages.FirstOrDefault(m => m.Messageid == id))
    .Where(m => m != null)
    .ToList();


            if (res2 != null)
            {
                return ultimateresults;
            }

            return finalresults.OrderByDescending(r => scores[r.Messageid]).Take(10).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET SEARCH RESULTS FAILED for user {UserId} and term {SearchTerm}", mainid, searchTerm);
            return Enumerable.Empty<SearchResultDto>();
        }

    }

    private async Task<string> GenerateEmbeddingAsync(string content)
    {
        using var client = new HttpClient();

        var request = new
        {
            text = content
        };

        var response = await client.PostAsJsonAsync(
            "http://localhost:8000/embed",
            request
        );

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<Dictionary<string, List<float>>>();

        var embedding = result!["embedding"];

        return JsonSerializer.Serialize(embedding);
    }
    private double CosineSimilarity(List<float> a, List<float> b)
    {
        if (a.Count != b.Count)
            throw new ArgumentException("Vectors must have the same dimensions.");

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < a.Count; i++)
        {
            dotProduct += a[i] * b[i];

            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private async Task<List<int>> GetReleventMessagesFromLLMAsync(Dictionary<int, List<Message>> relevantMessages, string searchTerm, int mainid)
    {
        _logger.LogInformation("GetRelevantMessagesFromLLMAsync method started");
        using var client = new HttpClient();

        var request = new
        {
            query = searchTerm,

            candidates = relevantMessages.Select(x => new
            {
                candidateMessageId = x.Key,

                messages = x.Value.Select(m => new
                {
                    id = m.MessageId,
                    sender = m.SenderId == mainid ? "Me" : "Other",
                    content = m.Content
                }).ToList()
            }).ToList()
        };

        var response = await client.PostAsJsonAsync(
            "http://localhost:8002/rank",
            request
        );

        response.EnsureSuccessStatusCode();

        var result =
        await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        var rankedResults = result!["results"];
        _logger.LogInformation("Ranked results received from LLM: {Results}", rankedResults.ToString());
        var messageIds = new List<int>();

        foreach (var item in rankedResults.EnumerateArray())
        {
            int messageId = item.GetProperty("messageId").GetInt32();
            double score = item.GetProperty("score").GetDouble();

            _logger.LogInformation("Ranked message {MessageId} with score {Score}", messageId, score);

            messageIds.Add(messageId);
        }
        _logger.LogInformation("GetRelevantMessagesFromLLMAsync method completed");
        return messageIds;

    }
}