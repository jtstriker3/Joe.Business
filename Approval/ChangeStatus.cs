﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joe.Business.Approval
{
    public enum ChangeStatus
    {
        Created,
        Submitted,
        AwaitingResults,
        Denied,
        Approved
    }
}
