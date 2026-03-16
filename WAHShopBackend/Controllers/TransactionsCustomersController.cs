using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsCustomersController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;

        [HttpGet("getTransactionsCustomers")]
        public async Task<IActionResult> GetTransactionsCustomers([FromQuery] GetItems<TransactionsCustomers> _getItems)
        {
            if (_getItems.AllItemsLoaded) { return Ok(new GetItems<TransactionsCustomers> { Items = [], AllItemsLoaded = true }); }

            GetItems<TransactionsCustomers> getItems = new ();
            try
            {
                var transactions = await _context.TransactionsCustomers
                    .Where(t => t.CustomerId == _getItems.Id)
                    .OrderByDescending(t => t.TransactionDate)
                    .Skip(_getItems.CurrentPage * _getItems.PageSize)
                    .Take(_getItems.PageSize)
                    .ToListAsync();

                if (transactions.Count == 0)
                {
                    getItems.AllItemsLoaded = true;
                }

                _getItems.CurrentPage++;

                getItems.Items = transactions;
                getItems.CurrentPage = _getItems.CurrentPage;

                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addTransaction")]
        public async Task<IActionResult> AddTransaction([FromBody] TransactionsCustomers transactionData)
        {
            try
            {

                _context.TransactionsCustomers.Add(transactionData);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    var debt = await _context.DebtCustomers
                               .FirstOrDefaultAsync(d => d.CustomerId == transactionData.CustomerId);

                    if (debt != null && debt.Balance == 0)
                    {
                        _context.DebtCustomers.Remove(debt);
                        await _context.SaveChangesAsync();

                        // -100 ist ein spezieller Wert, der anzeigt, dass die Transaktion erfolgreich hinzugefügt wurde, aber die ID nicht zurückgegeben werden kann, da sie von einem Trigger alle Transaktons gelöscht wurden, weil die Balance == 0.
                        return Ok(new ValidationResult { Result = true, Message = "Transaktion erfolgreich hinzugefügt, aber der Saldo 0 beträgt, deswegen wurden alle Transaktions von Trigger (trg_DeleteTransactionsOnDebtDelete) gelöscht", NewId = -100 });
                    }
                    else
                    {
                        return Ok(new ValidationResult {Result = true,Message = "Transaktion erfolgreich hinzugefügt",NewId = transactionData.Id});
                    }
                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Transaktion konnte nicht hinzugefügt werden." });

            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Ok(new ValidationResult
                {
                    Result = true,
                    Message = "Transaktion erfolgreich hinzugefügt."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getTransactionsCustomerById")]
        public async Task<IActionResult> GetTransactionsCustomersById(int id)
        {
            try
            {
                var transaction = await _context.TransactionsCustomers.FindAsync(id);
                if (transaction == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Transaktion nicht gefunden." });
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateTransaction")]
        public async Task<IActionResult> UpdateTransaction([FromBody] TransactionsCustomers editTransactionsCustomers)
        {
            var ifExist = await _context.TransactionsCustomers.FindAsync(editTransactionsCustomers.Id);
            if (ifExist == null)
                return NotFound(new ValidationResult { Result = false, Message = "Transaktion nicht gefunden." });
            try
            {
                // Validate input
                if (editTransactionsCustomers.Amount <= 0)
                    return BadRequest("Der Betrag darf nicht null oder negativ sein.");
                ifExist.CustomerId = editTransactionsCustomers.CustomerId;
                ifExist.Type = editTransactionsCustomers.Type;
                ifExist.Amount = editTransactionsCustomers.Amount;
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                    return Ok(new ValidationResult { Result = true, Message = "Transaktion erfolgreich aktualisiert." });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Transaktion konnte nicht aktualisiert werden." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteTransaction/{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.TransactionsCustomers.FindAsync(id);
            if (transaction == null)
                return NotFound(new ValidationResult { Result = false, Message = "Transaktion nicht gefunden." });
            try
            {
                _context.TransactionsCustomers.Remove(transaction);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                    return Ok(new ValidationResult { Result = true, Message = "Transaktion erfolgreich gelöscht." });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Transaktion konnte nicht gelöscht werden." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getTransactionsByCustomer/{customerId}")]
        public async Task<IActionResult> GetTransactionsByCustomer(int customerId)
        {
            try
            {
                var transactions = await _context.TransactionsCustomers
                    .Where(t => t.CustomerId == customerId)
                    .ToListAsync();
                if (transactions == null || transactions.Count == 0)
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Transaktionen für diesen Kunden gefunden." });

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
