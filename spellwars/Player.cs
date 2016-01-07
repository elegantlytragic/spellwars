using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpellWarsLibrary;
namespace SpellWarsClient
{
    public class Player
    {
        public List<Card> Hand;
        public List<TreasureCard> TreasureCards;
        protected HeroCard Hero;
        protected int Health;
        protected Texture2D[] CardTextures;
        public bool Dead;
        public string Name;
        public Player(HeroCard hero, Texture2D[] textures)
        {
            Hand = new List<Card>();
            TreasureCards = new List<TreasureCard>();
            Health = 20;
            Hero = hero;
            CardTextures = textures;
            Dead = false;
        }
        public void DrawCard(Card card)
        {
            Hand.Add(card);
        }
        public void DrawTreasureCard(TreasureCard card)
        {
            TreasureCards.Add(card);
        }
        public void Damage(int amount)
        {
            if (Health > Health - amount) Health -= amount;
            else Dead = true;
        }
        public void Heal(int amount)
        {
            if (Health + amount > 30) Health = 30;
            else Health += amount;
        }
        public string GetHeroName()
        {
            return Hero.Name;
        }
        public void DrawHand(SpriteBatch spriteBatch)
        {
            foreach (Card card in Hand) card.Draw(spriteBatch, CardTextures);
        }
        public void DrawHand(SpriteBatch spriteBatch, Vector2 position)
        {
            for (int i = 0; i < Hand.Count; i++) Hand[i].Draw(spriteBatch, CardTextures, new Vector2(position.X + i * 320, position.Y));
        }
        public void DrawHand(SpriteBatch spriteBatch, Rectangle position)
        {
            for (int i = 0; i < Hand.Count; i++) Hand[i].Draw(spriteBatch, CardTextures, new Rectangle(position.X + i * position.Width, position.Y, position.Width, position.Height));
        }
    }
}
