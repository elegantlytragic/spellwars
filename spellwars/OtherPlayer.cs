using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SpellWarsLibrary;
namespace SpellWarsClient
{
    class OtherPlayer : Player
    {
        private int numberOfCards;
        public OtherPlayer(HeroCard heroCard, Texture2D[] textures, string name, int health, int numOfCards) : base(heroCard, textures) 
        {
            Name = name;
            Health = health;
            numberOfCards = numOfCards;
        }
    }
}
