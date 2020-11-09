// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using TeamsConversationBot.SqlDatabase;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class TeamsConversationBotLogic : TeamsActivityHandler
    {
        private string _appId;
        private string _appPassword;
        private ISqlConnection sqlConnectionObj;
        public enum ParticipatingTeams { WAN, AppDelivery, SDN, PhyNet, DNS, ER, Monitoring, Datapath, PMs };

        public TeamsConversationBotLogic(IConfiguration config, ISqlConnection sqlConnection)
        {
            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            this.sqlConnectionObj = sqlConnection;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveRecipientMention();
            var text = turnContext.Activity.Text.Trim().ToLower();
            string distance;
            if ((distance = text.Split(" ").FirstOrDefault(x => float.TryParse(x, out float distance))) != null)
            {
                turnContext.Activity.Text = distance;
                await AddRecord(turnContext, cancellationToken);
            }
            else if (text.Contains("Add me", StringComparison.InvariantCultureIgnoreCase))
                await AddMember(turnContext, cancellationToken);
            else if (text.Contains("my total", StringComparison.InvariantCultureIgnoreCase))
                await SelfTotal(turnContext, cancellationToken);
            else if (text.Contains("team total", StringComparison.InvariantCultureIgnoreCase))
                await TeamTotal(turnContext, cancellationToken);
            else if (text.Contains("rank", StringComparison.InvariantCultureIgnoreCase))
                await GetRanking(turnContext, cancellationToken);
            else if (text.Contains("help", StringComparison.InvariantCultureIgnoreCase))
                await CardActivityAsync(turnContext, false, cancellationToken);
            else if (text.Contains("team stats for today", StringComparison.InvariantCultureIgnoreCase))
                await TeamStatsPerDay(turnContext, cancellationToken);
            else if (text.Contains("team stats", StringComparison.InvariantCultureIgnoreCase))
                await TeamStats(turnContext, cancellationToken);
            else
                await GetInfoActivityAsync(turnContext, cancellationToken);
        }

        private async Task CardActivityAsync(ITurnContext<IMessageActivity> turnContext, bool update, CancellationToken cancellationToken, string title = null)
        {
            var card = new HeroCard
            {
                Buttons = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Add me to --teamname-- team",
                                Text = "Add me"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Get Rank",
                                Text = "rank"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Get my team total",
                                Text = "team total"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Get my total",
                                Text = "my total"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Log --x-- km",
                                Text = "Log 0 km"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "team stats for today",
                                Text = "team stats for today"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Team stats",
                                Text = "team stats"
                            }
                        }
            };


            if (update)
            {
                await SendUpdatedCard(turnContext, card, cancellationToken);
            }
            else
            {
                await SendWelcomeCard(turnContext, card, cancellationToken, title);
            }

        }

        private static async Task SendWelcomeCard(ITurnContext<IMessageActivity> turnContext, HeroCard card, CancellationToken cancellationToken, string title=null)
        {
            var initialValue = new JObject { { "count", 0 } };
            var welcomeString = "Welcome friend! I am SimRUN. I will help you track your score for AzNet Give 2020 Virtual Run.";
            card.Title = string.IsNullOrEmpty(title)?
                 welcomeString:welcomeString + title;

            var activity = MessageFactory.Attachment(card.ToAttachment());

            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        private static async Task SendUpdatedCard(ITurnContext<IMessageActivity> turnContext, HeroCard card, CancellationToken cancellationToken)
        {
            card.Title = "I've been updated";

            var data = turnContext.Activity.Value as JObject;
            data = JObject.FromObject(data);
            data["count"] = data["count"].Value<int>() + 1;
            card.Text = $"Update count - {data["count"].Value<int>()}";

            card.Buttons.Add(new CardAction
            {
                Type = ActionTypes.MessageBack,
                Title = "Update Card",
                Text = "UpdateCardAction",
                Value = data
            });

            var activity = MessageFactory.Attachment(card.ToAttachment());
            activity.Id = turnContext.Activity.ReplyToId;

            await turnContext.UpdateActivityAsync(activity, cancellationToken);
        }

        private async Task GetInfoActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply;
            if (result.HasRows && result.Read())
            {
                reply = $"I see you are a member of {result.GetValue(1)} team.";
            }
            else
            {
                reply = $"Please register yourself in a team by typing 'Add me to --teamname-- team'";
            }

            await result.DisposeAsync();
            await CardActivityAsync(turnContext, false, cancellationToken, reply);
        }

        private async Task AddRecord(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}'";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply;
            if (result.HasRows)
            {
                await result.DisposeAsync();
                query = $"SELECT * FROM {SqlConnectionClass.RecordTable} WHERE Id='{turnContext.Activity.From.Id}' AND DateOfRecord='{DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}'";
                result = sqlConnectionObj.GetQueryResult(query);
                if (result.HasRows)
                {
                    query = $"UPDATE {SqlConnectionClass.RecordTable} SET Distance='{turnContext.Activity.Text}', ModifiedTime='{DateTime.UtcNow.TimeOfDay}'" +
                        $" WHERE Id='{turnContext.Activity.From.Id}' AND DateOfRecord='{DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}'";
                }
                else
                {
                    query = $"INSERT INTO {SqlConnectionClass.RecordTable} VALUES ('{XmlConvert.EncodeName(turnContext.Activity.From.Name)}'," +
                        $"'{DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}','{DateTime.UtcNow.TimeOfDay}','{turnContext.Activity.Text}','Manual'," +
                        $"'{turnContext.Activity.From.Id}');";
                }

                await result.DisposeAsync();
                if (sqlConnectionObj.ExecuteQuery(query) > 0)
                    reply = $"Your record has been sucessfully added. Entry Summary: {turnContext.Activity.Text} km on {DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}.";
                else
                    reply = SqlConnectionClass.FailureString;
            }
            else
            {
                reply = "Please register yourself in a team by typing 'Add me to --teamname-- team'";
            }

            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);
            //replyActivity.Entities = new List<Entity> { mention };

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task AddMember(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = string.Empty;
            if (result.HasRows && result.Read())
            {
                    reply = $"You are already added to team {result.GetValue(1)}";
            }
            else
            {
                ParticipatingTeams teamname;
                if (Enum.TryParse<ParticipatingTeams>(turnContext.Activity.Text.Split(" ")[3], true, out teamname))
                {
                    query = $"INSERT INTO {SqlConnectionClass.MemberTable} VALUES ('{XmlConvert.EncodeName(turnContext.Activity.From.Name)}'" +
                        $",'{teamname.ToString()}','{turnContext.Activity.From.Id}');";
                    await result.DisposeAsync();
                    if (sqlConnectionObj.ExecuteQuery(query)>0)
                    {
                        reply = $"Sucessfully registered you to team {teamname.ToString()}";
                    }
                    else
                    {
                        reply = SqlConnectionClass.FailureString;
                    }
                }
                else
                {
                    reply = "Please enter valid team name from list" + string.Join(",", Enum.GetNames(typeof(ParticipatingTeams)));
                    reply += "Register yourself in a team by typing 'Add me to --teamname-- team'";
                }
            }

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);
            //replyActivity.Entities = new List<Entity> { mention };

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task SelfTotal(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = string.Empty;
            if (result.HasRows && result.Read())
            {
                await result.DisposeAsync();
                query = $"SELECT SUM(Distance) FROM {SqlConnectionClass.RecordTable} WHERE Id='{turnContext.Activity.From.Id}';";
                reply = $"Your personal total is {sqlConnectionObj.GetQueryValue<double>(query)}km";
            }
            else
            {
                reply = "Your personal total is 0km";
            }

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task TeamTotal(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = string.Empty;
            if (result.HasRows && result.Read())
            {
                var teamname = result.GetValue(1);
                await result.DisposeAsync();
                query = $"SELECT SUM(Distance) FROM {SqlConnectionClass.RecordTable} " +
                    $"INNER JOIN {SqlConnectionClass.MemberTable} ON {SqlConnectionClass.RecordTable}.Id = {SqlConnectionClass.MemberTable}.Id WHERE TeamName='{teamname}'";
                reply = $"Your team total is {sqlConnectionObj.GetQueryValue<double>(query)}km";
            }
            else
            {
                reply = "Your team total is 0km";
            }

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task TeamStats(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = " Your team stats:";
            if (result.HasRows && result.Read())
            {
                var teamname = result.GetValue(1);
                await result.DisposeAsync();
                query = $"SELECT {SqlConnectionClass.MemberTable}.MemberName, SUM(Distance) FROM {SqlConnectionClass.RecordTable} " +
                    $"INNER JOIN {SqlConnectionClass.MemberTable} ON {SqlConnectionClass.RecordTable}.Id = {SqlConnectionClass.MemberTable}.Id WHERE TeamName='{teamname}' " +
                    $"GROUP BY {SqlConnectionClass.MemberTable}.MemberName " +
                    $"ORDER BY {SqlConnectionClass.MemberTable}.MemberName ASC;";

                result = sqlConnectionObj.GetQueryResult(query);
                reply += GetTableText(result);
            }
            else
            {
                reply = "Your team total is 0km";
            }

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task TeamStatsPerDay(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT * FROM {SqlConnectionClass.MemberTable} WHERE Id='{turnContext.Activity.From.Id}';";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = "Your team stats for today :";
            if (result.HasRows && result.Read())
            {
                var teamname = result.GetValue(1);
                await result.DisposeAsync();
                query = $"SELECT {SqlConnectionClass.MemberTable}.MemberName, Distance FROM {SqlConnectionClass.RecordTable} " +
                    $"INNER JOIN {SqlConnectionClass.MemberTable} ON {SqlConnectionClass.RecordTable}.Id = {SqlConnectionClass.MemberTable}.Id " +
                    $"WHERE TeamName='{teamname}' AND DateOfRecord='{DateTime.UtcNow.Date.ToString("MM/dd/yyyy")}'" +
                    $" ORDER BY {SqlConnectionClass.MemberTable}.MemberName ASC";

                result = sqlConnectionObj.GetQueryResult(query);
                reply += GetTableText(result);
            }
            else
            {
                reply = "Your team total is 0km";
            }

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private async Task GetRanking(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var query = $"SELECT {SqlConnectionClass.MemberTable}.teamname, dist = sum(distance)/avg(membercount) FROM {SqlConnectionClass.MemberTable}" +
                $" INNER JOIN {SqlConnectionClass.RecordTable} ON {SqlConnectionClass.MemberTable}.Id = {SqlConnectionClass.RecordTable}.Id " +
                $"INNER JOIN {SqlConnectionClass.TeamsTable} ON {SqlConnectionClass.TeamsTable}.teamname={SqlConnectionClass.MemberTable}.teamname " +
                $"GROUP BY {SqlConnectionClass.MemberTable}.teamname ORDER BY dist DESC;";
            var result = sqlConnectionObj.GetQueryResult(query);
            string reply = "Current Ranking Stands as :";
            reply = GetTableText(result);

            await result.DisposeAsync();
            SetReply(ref reply, turnContext);
            var replyActivity = MessageFactory.Text(reply);
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        private void SetReply(ref string reply, ITurnContext cxt)
        {
            reply = XmlConvert.DecodeName(XmlConvert.EncodeName($"Hello {cxt.Activity.From.Name}. " + reply));
        }

        private string GetTableText(System.Data.SqlClient.SqlDataReader reader)
        {
            var reply = "<table>";
            while (reader.Read())
            {
                reply += "<tr>";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i);
                    if(val.GetType()==typeof(string))
                    {
                        reply += $"<td>{XmlConvert.DecodeName(val.ToString())}</td>";
                    }
                    else
                    {
                        reply += $"<td>{reader.GetValue(i)}</td>";
                    }
                }
                reply += "</tr>";
            }

            reply += "</table>";
            return reply;
        }

        private async Task getTotalRecord()
        {
            string query1 = $"select membername,teamname from {SqlConnectionClass.MemberTable}";
            var datareader = sqlConnectionObj.GetQueryResult(query1);

            List<string> members = new List<string>();
            while(datareader.Read())
            {
                members.Add(datareader.GetValue(0).ToString());
            }

            await datareader.DisposeAsync();

            string query2 = $"select ";


        }
    }
}
