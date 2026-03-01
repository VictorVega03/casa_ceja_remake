using System;
using System.Collections.Generic;
using CasaCejaRemake.Models;
using CasaCejaRemake.ViewModels.POS;

namespace CasaCejaRemake.Services.Interfaces
{
    /// <summary>
    /// Contrato público del servicio de generación de tickets.
    /// </summary>
    public interface ITicketService
    {
        string GenerateFolio(int branchId, int consecutivo);
        string GenerateCreditFolio(int branchId, int consecutivo);
        string GenerateLayawayFolio(int branchId, int consecutivo);
        string GetPaymentMethodName(PaymentMethod method);

        TicketData GenerateTicket(
            string folio,
            int branchId,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            int userId,
            string userName,
            List<CartItem> items,
            PaymentMethod paymentMethod,
            decimal amountPaid,
            decimal change,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true);

        TicketData GenerateTicketWithMixedPayment(
            string folio,
            int branchId,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            int userId,
            string userName,
            List<CartItem> items,
            string paymentJson,
            decimal totalPaid,
            decimal change,
            decimal generalDiscount = 0,
            decimal generalDiscountPercent = 0,
            bool isGeneralDiscountPercentage = true);

        TicketData GenerateCreditTicket(
            string folio,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            string userName,
            string customerName,
            string customerPhone,
            List<CartItem> items,
            decimal total,
            decimal initialPayment,
            decimal remainingBalance,
            DateTime dueDate,
            int monthsToPay,
            PaymentMethod paymentMethod);

        TicketData GenerateLayawayTicket(
            string folio,
            string branchName,
            string branchAddress,
            string branchPhone,
            string branchRazonSocial,
            string userName,
            string customerName,
            string customerPhone,
            List<CartItem> items,
            decimal total,
            decimal initialPayment,
            decimal remainingBalance,
            DateTime pickupDate,
            int daysToPickup,
            PaymentMethod paymentMethod);

        string SerializeTicket(TicketData ticket);
        TicketData? DeserializeTicket(string json);

        // GenerateTicketText con sobrecargas exactas
        string GenerateTicketText(TicketData ticket, int lineWidth = 32);
        string GenerateTicketText(TicketData ticket, TicketType type, int lineWidth = 32);
        string GenerateTicketText(TicketData ticket, TicketType type, string rfc, string ticketFooter, int lineWidth = 32);

        string GeneratePaymentTicketText(PaymentTicketData data, string rfc = "", int lineWidth = 32);

        string GenerateReprintWithHistoryText(
            TicketData ticket,
            TicketType type,
            List<string>? paymentDetailsJsonList,
            decimal totalPaid,
            string rfc = "",
            int lineWidth = 32);

        string GenerateHistoryTicketText(
            TicketData ticket,
            TicketType type,
            List<PaymentHistoryItem> paymentHistory,
            decimal totalPaid,
            string rfc = "",
            int lineWidth = 32);

        string GenerateCashCloseTicketText(
            string branchName,
            string branchAddress,
            string branchPhone,
            string folio,
            string userName,
            DateTime openingDate,
            DateTime closeDate,
            decimal openingCash,
            decimal totalCash,
            decimal totalDebit,
            decimal totalCredit,
            decimal totalTransfer,
            decimal totalChecks,
            decimal layawayCash,
            decimal creditCash,
            decimal creditTotalCreated,
            decimal layawayTotalCreated,
            decimal totalExpenses,
            decimal totalIncome,
            decimal expectedCash,
            decimal declaredAmount,
            decimal difference,
            int salesCount,
            List<(string Concept, decimal Amount)>? expenses = null,
            List<(string Concept, decimal Amount)>? incomes = null,
            int lineWidth = 32);
    }
}
