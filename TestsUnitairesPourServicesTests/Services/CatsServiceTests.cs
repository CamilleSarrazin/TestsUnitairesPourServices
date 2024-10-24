using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestsUnitairesPourServices.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestsUnitairesPourServices.Data;
using TestsUnitairesPourServices.Models;
using TestsUnitairesPourServices.Exceptions;

namespace TestsUnitairesPourServices.Services.Tests
{
    [TestClass()]
    public class CatsServiceTests
    {
        DbContextOptions<ApplicationDBContext> options;

        //Utilisez des constantes pour les IDs de vos tests, on veut éviter d'utiliser des chiffres magiques
        private const int CLEAN_HOUSE_ID = 1;
        private const int DIRTY_HOUSE_ID = 2;

        private const int WILD_CAT_ID = 1;
        private const int CAT_IN_DIRTY_HOUSE_ID = 2;
        public CatsServiceTests()
        {
            // TODO On initialise les options de la BD, on utilise une InMemoryDatabase
            options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "CatsService")
                .UseLazyLoadingProxies(true)
                .Options;
        }


        [TestInitialize]
        public void Init()
        {
            // TODO avoir la durée de vie d'un context la plus petite possible
            using ApplicationDBContext db = new ApplicationDBContext(options);

            // TODO on ajoute des données de tests
            db.Cat.Add(new Models.Cat()
            {
                Id = WILD_CAT_ID,
                Name = "Sauvage",
                Age = 14
            });

            House maisonPropre = new House()
            {
                Id = CLEAN_HOUSE_ID,
                Address = "Maison propre toute blanche",
                OwnerName = "BlingBling"
            };

            House maisonSale = new House()
            {
                Id = DIRTY_HOUSE_ID,
                Address = "Maison vraiment beurk",
                OwnerName = "Bruh"
            };

            db.House.Add(maisonPropre);
            db.House.Add(maisonSale);

            Cat chatPasPropre = new Cat()
            {
                Id = CAT_IN_DIRTY_HOUSE_ID,
                Name = "JePue",
                Age = 5,
                House = maisonSale
            };

            db.Cat.Add(chatPasPropre);
            db.SaveChanges();
        }

        [TestCleanup]
        public void Dispose()
        {
            //TODO on efface les données de tests pour remettre la BD dans son état initial
            using ApplicationDBContext db = new ApplicationDBContext(options);
            db.Cat.RemoveRange(db.Cat);
            db.House.RemoveRange(db.House);
            db.SaveChanges();
        }

        [TestMethod]
        public void MoveTest()
        {
            using ApplicationDBContext db = new ApplicationDBContext(options);
            var catsService = new CatsService(db);
            var maisonPropre = db.House.Find(CLEAN_HOUSE_ID);
            var maisonSale = db.House.Find(DIRTY_HOUSE_ID);

            // Tout est bon, le chat va être dans une maison propre
            var chatMaintenantPropre = catsService.Move(CAT_IN_DIRTY_HOUSE_ID, maisonSale, maisonPropre);
            Assert.IsNotNull(chatMaintenantPropre);
        }

        [TestMethod]
        public void MoveTestCatNotFound()
        {
            using ApplicationDBContext db = new ApplicationDBContext(options);
            var catsService = new CatsService(db);
            var maisonPropre = db.House.Find(CLEAN_HOUSE_ID);
            var maisonSale = db.House.Find(DIRTY_HOUSE_ID);

            //Retourne null si le chat ne peut pas être trouvé (aucun chat avec Id: 42)
            var chat = catsService.Move(42, maisonSale, maisonPropre);
            Assert.IsNull(chat);
        }

        [TestMethod]
        public void MoveTestNoHouse()
        {
            using ApplicationDBContext db = new ApplicationDBContext(options);
            var catsService = new CatsService(db);
            var maisonPropre = db.House.Find(CLEAN_HOUSE_ID);
            var maisonSale = db.House.Find(DIRTY_HOUSE_ID);

            //Le chat avec l'Id 1 n'a pas de maison
            Exception e = Assert.ThrowsException<WildCatException>(() => catsService.Move(WILD_CAT_ID, maisonSale, maisonPropre));
            Assert.AreEqual("On n'apprivoise pas les chats sauvages", e.Message);
        }

        [TestMethod]
        public void MoveTestWrongHouse()
        {
            using ApplicationDBContext db = new ApplicationDBContext(options);
            var catsService = new CatsService(db);
            var maisonPropre = db.House.Find(CLEAN_HOUSE_ID);
            var maisonSale = db.House.Find(DIRTY_HOUSE_ID);

            //Les maisons sont inversées
            Exception e = Assert.ThrowsException<DontStealMyCatException>(() => catsService.Move(CAT_IN_DIRTY_HOUSE_ID, maisonPropre, maisonSale));
            Assert.AreEqual("Touche pas à mon chat!", e.Message);
        }
    }
}