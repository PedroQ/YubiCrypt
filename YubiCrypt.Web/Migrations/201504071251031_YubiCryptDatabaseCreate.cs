namespace YubiCrypt.Web.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class YubiCryptDatabaseCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.YubiKeys",
                c => new
                    {
                        YubiKeyUID = c.String(nullable: false, maxLength: 128),
                        Privateidentity = c.String(),
                        AESKey = c.String(),
                        Active = c.Boolean(nullable: false),
                        Counter = c.Int(nullable: false),
                        Time = c.Int(nullable: false),
                        DateAdded = c.DateTime(nullable: false),
                        SerialNumber = c.Int(nullable: false),
                        YubikeyVersion = c.String(),
                        NDEFEnabled = c.Boolean(nullable: false),
                        UserProfile_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.YubiKeyUID)
                .ForeignKey("dbo.AspNetUsers", t => t.UserProfile_Id)
                .Index(t => t.UserProfile_Id);
            
            CreateTable(
                "dbo.TFKDSecrets",
                c => new
                    {
                        YubiKeyUID = c.String(nullable: false, maxLength: 128),
                        TFKDEncryptedSecret = c.String(),
                        TFKDEncryptionSalt = c.String(),
                    })
                .PrimaryKey(t => t.YubiKeyUID)
                .ForeignKey("dbo.YubiKeys", t => t.YubiKeyUID)
                .Index(t => t.YubiKeyUID);
            
            AddColumn("dbo.AspNetUsers", "MeoCloudAPIToken", c => c.String());
            AddColumn("dbo.AspNetUsers", "MeoCloudAPISecret", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.YubiKeys", "UserProfile_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.TFKDSecrets", "YubiKeyUID", "dbo.YubiKeys");
            DropIndex("dbo.TFKDSecrets", new[] { "YubiKeyUID" });
            DropIndex("dbo.YubiKeys", new[] { "UserProfile_Id" });
            DropColumn("dbo.AspNetUsers", "MeoCloudAPISecret");
            DropColumn("dbo.AspNetUsers", "MeoCloudAPIToken");
            DropTable("dbo.TFKDSecrets");
            DropTable("dbo.YubiKeys");
        }
    }
}
