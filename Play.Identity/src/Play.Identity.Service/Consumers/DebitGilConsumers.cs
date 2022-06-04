using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Play.Identity.Contracts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;

namespace Play.Identity.Service.Consumers
{
    public class DebitGilConsumers : IConsumer<DebitGil>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public DebitGilConsumers(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task Consume(ConsumeContext<DebitGil> context)
        {
            var message = context.Message;
            var user = await _userManager.FindByIdAsync(message.UserId.ToString());
            if(user == null)
            {
                throw new UnknownUserException(message.UserId);
            }

            user.Gil -= message.Gil;
            if(user.Gil < 0)
            {
                throw new InsufficientFundsException(message.UserId, message.Gil);
            }
            await _userManager.UpdateAsync(user);

            await context.Publish(new GilDebited(message.CorrelationId));
        }
    }
}