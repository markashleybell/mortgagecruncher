﻿using System;
using System.Collections.Generic;

namespace mortgagecruncher.Models
{
    public class AmortisationSchedule
    {
        List<AmortisationScheduleEntry> _scheduleEntries = new List<AmortisationScheduleEntry>();
        public IEnumerable<AmortisationScheduleEntry> ScheduleEntries { get { return _scheduleEntries; } }

        public AmortisationSchedule(AmortisationScheduleType type, DateTime startDate, decimal value, int termMonths, decimal termRate, int fixedTermMonths = 0, decimal fixedTermRate = 0, int extraPaymentInterval = 0, decimal extraPaymentAmount = 0)
        {
            switch(type)
            {
                case AmortisationScheduleType.FixedTerm:
                    CalculateFixedTermAmortisationSchedule(_scheduleEntries, startDate, value, termMonths, termRate, fixedTermMonths, fixedTermRate, extraPaymentInterval, extraPaymentAmount);
                    break;
                case AmortisationScheduleType.FixedPayments:
                    CalculateFixedPaymentAmortisationSchedule(_scheduleEntries, startDate, value, termMonths, termRate, fixedTermMonths, fixedTermRate, extraPaymentInterval, extraPaymentAmount);
                    break;
            }
        }

        #region Fixed Payments

        private void CalculateFixedPaymentAmortisationSchedule(List<AmortisationScheduleEntry> scheduleEntries, DateTime startDate, decimal value, int termMonths, decimal termRate, int fixedTermMonths = 0, decimal fixedTermRate = 0, int extraPaymentInterval = 0, decimal extraPaymentAmount = 0)
        {
            int fullTermMonths = termMonths;
            int variableTermMonths = termMonths - fixedTermMonths;
            decimal balance = value;
            decimal e = 0;

            // Add a month because the first payment won't go out until the following month
            DateTime date = startDate.AddMonths(1);

            int i = 1;

            var fixedTermEntry = CalculateFixedTermAmortisationScheduleEntry(value, fullTermMonths, InterestType.Fixed, fixedTermRate, i, date, balance, 0);
            
            var payment = fixedTermEntry.Payment;

            // Calculate the schedule entries for the fixed term period
            while(Math.Floor(balance) > 0 && i <= fixedTermMonths)
            {
                e = (extraPaymentInterval > 0 && (i % extraPaymentInterval == 0)) ? extraPaymentAmount : 0;

                var entry = CalculateFixedPaymentAmortisationScheduleEntry(payment, value, fullTermMonths, InterestType.Fixed, fixedTermRate, i, date, balance, e);
                scheduleEntries.Add(entry);
                if(e > 0)
                { 
                     value = entry.Balance;
                     fullTermMonths = termMonths - i;
                }
                balance = entry.Balance;
                date = date.AddMonths(1);
                i++;
            }

            decimal remainingValue = balance;

            var variableTermEntry = CalculateFixedTermAmortisationScheduleEntry(remainingValue, variableTermMonths, InterestType.Variable, termRate, i, date, balance, 0);

            payment = variableTermEntry.Payment;

            // Calculate the schedule entries for the remaining period (or the whole term if there was no fixed period)
            while(Math.Floor(balance) > 0)
            {
                e = (extraPaymentInterval > 0 && (i % extraPaymentInterval == 0)) ? extraPaymentAmount : 0;

                var entry = CalculateFixedPaymentAmortisationScheduleEntry(payment, remainingValue, variableTermMonths, InterestType.Variable, termRate, i, date, balance, e);
                scheduleEntries.Add(entry);
                if(e > 0)
                {
                    remainingValue = entry.Balance;
                    variableTermMonths = termMonths - i;
                }
                balance = entry.Balance;
                date = date.AddMonths(1);
                i++;
            }
        }

        private AmortisationScheduleEntry CalculateFixedPaymentAmortisationScheduleEntry(decimal payment, decimal value, int term, InterestType interestType, decimal rate, int paymentNumber, DateTime paymentDate, decimal balance, decimal extraPaymentAmount = 0)
        {
            var i = MonthlyInterestRate(rate);

            if(extraPaymentAmount > 0 && (payment + extraPaymentAmount) <= balance)
                 payment += extraPaymentAmount;

            var interest = i * balance;

            if((payment + interest) > balance)
                payment = balance + interest;

            var principal = payment - interest;

            var newbalance = ((balance - payment) + interest);

            return new AmortisationScheduleEntry(paymentNumber, paymentDate, payment, principal, interest, interestType, rate, newbalance);
        }

        #endregion Fixed Payments

        #region Fixed Term

        private void CalculateFixedTermAmortisationSchedule(List<AmortisationScheduleEntry> scheduleEntries, DateTime startDate, decimal value, int termMonths, decimal termRate, int fixedTermMonths = 0, decimal fixedTermRate = 0, int extraPaymentInterval = 0, decimal extraPaymentAmount = 0)
        {
            int fullTermMonths = termMonths;
            int variableTermMonths = termMonths - fixedTermMonths;
            decimal balance = value;
            decimal e = 0;

            // Add a month because the first payment won't go out until the following month
            DateTime date = startDate.AddMonths(1);

            int i = 1;

            // Calculate the schedule entries for the fixed term period
            for(; i <= fixedTermMonths; i++)
            {
                e = (extraPaymentInterval > 0 && (i % extraPaymentInterval == 0)) ? extraPaymentAmount : 0;

                var entry = CalculateFixedTermAmortisationScheduleEntry(value, fullTermMonths, InterestType.Fixed, fixedTermRate, i, date, balance, e);
                scheduleEntries.Add(entry);
                if(e > 0)
                { 
                     value = entry.Balance;
                     fullTermMonths = termMonths - i;
                }
                balance = entry.Balance;
                date = date.AddMonths(1);
            }

            decimal remainingValue = balance;

            // Calculate the schedule entries for the remaining period (or the whole term if there was no fixed period)
            for(; i <= termMonths; i++)
            {
                e = (extraPaymentInterval > 0 && (i % extraPaymentInterval == 0)) ? extraPaymentAmount : 0;

                var entry = CalculateFixedTermAmortisationScheduleEntry(remainingValue, variableTermMonths, InterestType.Variable, termRate, i, date, balance, e);
                scheduleEntries.Add(entry);

                if(e > 0)
                {
                    remainingValue = entry.Balance;
                    variableTermMonths = termMonths - i;
                }
                balance = entry.Balance;
                date = date.AddMonths(1);
            }
        }

        private AmortisationScheduleEntry CalculateFixedTermAmortisationScheduleEntry(decimal value, int term, InterestType interestType, decimal rate, int paymentNumber, DateTime paymentDate, decimal balance, decimal extraPaymentAmount = 0)
        {
            var i = MonthlyInterestRate(rate);
            var a = Power(1.00M + i, term);

            var payment = value * ((i * a) / (a - 1.00M));

            if(extraPaymentAmount > 0 && (payment + extraPaymentAmount) <= balance)
                 payment += extraPaymentAmount;

            var interest = i * balance;
            var principal = payment - interest;

            var newbalance = ((balance - payment) + interest);

            return new AmortisationScheduleEntry(paymentNumber, paymentDate, payment, principal, interest, interestType, rate, newbalance);
        }

        #endregion Fixed Term

        private static decimal Power(decimal val, int pow)
        {
            decimal ret = 1;
            for(int i = 0; i < pow; i++)
                ret *= val;
            return ret;
        }

        private decimal MonthlyInterestRate(decimal termRate)
        {
            return termRate / 100.00M / 12.00M;
        }
    }
}