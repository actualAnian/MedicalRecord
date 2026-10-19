using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Register
{
    internal class ChangeStatusDecider
    {
        public static Outcome<object> Decide(
            object command, object state, DateTime today)
        {
            return (command, state) switch
            {
                _ => new Outcome<object>()
            };
        }
    }

    public class Outcome<T>
    {

    }
}