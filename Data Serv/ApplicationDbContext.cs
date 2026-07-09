using Application_Camion_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Application_Camion_API.Data;


public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }



    public DbSet<Utilisateur> Utilisateurs { get; set; }

    public DbSet<Tournee> Tournees { get; set; }

    public DbSet<Etape> Etapes { get; set; }

    public DbSet<Vehicule> Vehicules { get; set; }

    public DbSet<ModeleVehicule> ModelesVehicules { get; set; }

    public DbSet<CamionPorteur> CamionsPorteurs { get; set; }



    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);



        modelBuilder.Entity<Tournee>()
            .HasMany(t => t.Etapes)
            .WithOne(e => e.Tournee)
            .HasForeignKey(e => e.TourneeId)
            .OnDelete(DeleteBehavior.Cascade);



        modelBuilder.Entity<Etape>()
            .HasMany(e => e.Vehicules)
            .WithOne(v => v.Etape)
            .HasForeignKey(v => v.EtapeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tournee>()
            .HasOne(t => t.CamionPorteur)
            .WithMany(c => c.Tournees)
            .HasForeignKey(t => t.CamionPorteurId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vehicule>()
            .HasOne(v => v.ModeleVehicule)
            .WithMany(m => m.Vehicules)
            .HasForeignKey(v => v.ModeleVehiculeId)
            .OnDelete(DeleteBehavior.SetNull);



        modelBuilder.Entity<Tournee>()
            .Property(t => t.CodeUnique)
            .IsRequired();

        modelBuilder.Entity<Tournee>()
            .HasIndex(t => t.CodeUnique)
            .IsUnique();



        modelBuilder.Entity<Utilisateur>()
            .Property(u => u.Nom)
            .IsRequired();

        modelBuilder.Entity<Utilisateur>()
            .HasIndex(u => u.Nom)
            .IsUnique();

        modelBuilder.Entity<ModeleVehicule>()
            .HasIndex(m => new { m.Marque, m.Modele })
            .IsUnique();

    }

}
