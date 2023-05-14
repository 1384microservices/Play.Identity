using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Play.Common.Configuration;
using Play.Identity.Contracts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;

namespace Play.Identity.Service.Consumers;

public class DebitGilConsumer : IConsumer<DebitGil>
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<DebitGilConsumer> logger;
    private readonly Counter<int> _gilDebitedCounter;

    public DebitGilConsumer(UserManager<ApplicationUser> userManager, ILogger<DebitGilConsumer> logger, IConfiguration configuration)
    {
        this.userManager = userManager;
        this.logger = logger;
        var meter = new Meter(configuration.GetServiceSettings().Name);
        _gilDebitedCounter = meter.CreateCounter<int>("GilDebited");
    }

    public async Task Consume(ConsumeContext<DebitGil> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Debited {Gil} gil from user {UserId} with {CorrelationId}", 
            message.Gil, 
            message.UserId, 
            message.CorrelationId);

        var user = await userManager.FindByIdAsync(message.UserId.ToString());

        if (user == null)
        {
            throw new UnknownUserException(message.UserId);
        }


        if (user.MessageIds.Contains(context.MessageId.Value))
        {
            await context.Publish(new GilDebited(message.CorrelationId));
            return;
        }

        user.Gil -= message.Gil;

        if (user.Gil < 0)
        {
            logger.LogError(
                "Failed debit {Gil} gil from user {UserId} with {CorrelationId}", 
                message.Gil, 
                message.UserId, 
                message.CorrelationId);
            throw new InsufficientFundsException(message.UserId, message.Gil);
        }

        user.MessageIds.Add(context.MessageId.Value);


        await userManager.UpdateAsync(user);

        var userUpdatedTask = context.Publish(new UserUpdated(user.Id, user.Email, user.Gil));
        var gilDebitedTask = context.Publish(new GilDebited(message.CorrelationId));
        await Task.WhenAll(userUpdatedTask, gilDebitedTask);
        _gilDebitedCounter.Add(1);
    }
}