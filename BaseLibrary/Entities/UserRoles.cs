﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Entities
{
    public class UserRoles
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int UserId {get; set; }
    }
}
