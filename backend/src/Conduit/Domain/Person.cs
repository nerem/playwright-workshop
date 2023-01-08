using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Conduit.Domain
{
    public class Person
    {
        [JsonIgnore]
        public int PersonId { get; set; }

        public string? Username { get; set; }

        public string? Email { get; set; }

        public string? Bio { get; set; }

        public string? Image { get; set; }

        [JsonIgnore]
        public List<ArticleFavorite> ArticleFavorites { get; set; } = new();

        [JsonIgnore]
        public List<FollowedPeople> FollowingPersons { get; set; } = new();

        [NotMapped]
        public bool Following { get; set; } = false;

        [JsonIgnore]
        public List<FollowedPeople> FollowerPersons { get; set; } = new();

        [JsonIgnore]
        public byte[] Hash { get; set; } = Array.Empty<byte>();

        [JsonIgnore]
        public byte[] Salt { get; set; } = Array.Empty<byte>();
    }
}
