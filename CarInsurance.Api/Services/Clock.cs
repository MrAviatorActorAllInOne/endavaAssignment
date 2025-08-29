﻿namespace CarInsurance.Api.Services
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
