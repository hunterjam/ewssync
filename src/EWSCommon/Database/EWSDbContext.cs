﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EWS.Common.Database
{
    /// <summary>
    /// point to the class that inherit from DbConfiguration
    /// </summary>
    [DbConfigurationType(typeof(EWSDbConfig))]
    public class EWSDbContext : DbContext
    {
        public EWSDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            this.Configuration.ProxyCreationEnabled = false;

            System.Data.Entity.Database.SetInitializer<EWSDbContext>(null);
            Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EntitySubscription>().HasKey(hk => hk.Id);

            modelBuilder.Entity<EntityRoomListRoom>().HasKey(hk => hk.Id);

            modelBuilder.Entity<EntityRoomAppointment>().HasKey(hk => hk.Id);
            modelBuilder.Entity<EntityRoomAppointment>().HasRequired(hr => hr.Room).WithMany(wm => wm.Appointments).HasForeignKey(fk => fk.RoomId);
        }

        public DbSet<EntityRoomListRoom> RoomListRoomEntities { get; set; }


        public DbSet<EntityRoomAppointment> AppointmentEntities { get; set; }


        public DbSet<EntitySubscription> SubscriptionEntities { get; set; }
    }
}
