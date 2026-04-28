using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Services;

public class ScoringService : IScoringService
{
    // ------------------------------------------------------------------
    // Group Score Table — 27 categories × 4 companions = 108 values
    // ------------------------------------------------------------------
    private static readonly Dictionary<(PlaceCategory Category, TravelCompanion Companion), int> GroupScoreTable = new()
    {
        // Aquarium
        { (PlaceCategory.Aquarium, TravelCompanion.Solo), 0 },
        { (PlaceCategory.Aquarium, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Aquarium, TravelCompanion.Family), 20 },
        { (PlaceCategory.Aquarium, TravelCompanion.Friends), 10 },
        // Attraction
        { (PlaceCategory.Attraction, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Attraction, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Attraction, TravelCompanion.Family), 10 },
        { (PlaceCategory.Attraction, TravelCompanion.Friends), 10 },
        // Bar
        { (PlaceCategory.Bar, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Bar, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Bar, TravelCompanion.Family), -20 },
        { (PlaceCategory.Bar, TravelCompanion.Friends), 20 },
        // Beach
        { (PlaceCategory.Beach, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Beach, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Beach, TravelCompanion.Family), 10 },
        { (PlaceCategory.Beach, TravelCompanion.Friends), 20 },
        // Bridge
        { (PlaceCategory.Bridge, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Bridge, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Bridge, TravelCompanion.Family), 0 },
        { (PlaceCategory.Bridge, TravelCompanion.Friends), 10 },
        // Cafe
        { (PlaceCategory.Cafe, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Cafe, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Cafe, TravelCompanion.Family), 10 },
        { (PlaceCategory.Cafe, TravelCompanion.Friends), 10 },
        // Castle
        { (PlaceCategory.Castle, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Castle, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Castle, TravelCompanion.Family), 10 },
        { (PlaceCategory.Castle, TravelCompanion.Friends), 10 },
        // Cave
        { (PlaceCategory.Cave, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Cave, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Cave, TravelCompanion.Family), -10 },
        { (PlaceCategory.Cave, TravelCompanion.Friends), 10 },
        // Church
        { (PlaceCategory.Church, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Church, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Church, TravelCompanion.Family), 10 },
        { (PlaceCategory.Church, TravelCompanion.Friends), 10 },
        // Forest
        { (PlaceCategory.Forest, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Forest, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Forest, TravelCompanion.Family), 0 },
        { (PlaceCategory.Forest, TravelCompanion.Friends), 10 },
        // Gallery
        { (PlaceCategory.Gallery, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Gallery, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Gallery, TravelCompanion.Family), 0 },
        { (PlaceCategory.Gallery, TravelCompanion.Friends), 0 },
        // Historical
        { (PlaceCategory.Historical, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Historical, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Historical, TravelCompanion.Family), 10 },
        { (PlaceCategory.Historical, TravelCompanion.Friends), 10 },
        // Information
        { (PlaceCategory.Information, TravelCompanion.Solo), 0 },
        { (PlaceCategory.Information, TravelCompanion.Couple), 0 },
        { (PlaceCategory.Information, TravelCompanion.Family), 0 },
        { (PlaceCategory.Information, TravelCompanion.Friends), 0 },
        // Mall
        { (PlaceCategory.Mall, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Mall, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Mall, TravelCompanion.Family), 10 },
        { (PlaceCategory.Mall, TravelCompanion.Friends), 20 },
        // Market
        { (PlaceCategory.Market, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Market, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Market, TravelCompanion.Family), 10 },
        { (PlaceCategory.Market, TravelCompanion.Friends), 10 },
        // Memorial
        { (PlaceCategory.Memorial, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Memorial, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Memorial, TravelCompanion.Family), 10 },
        { (PlaceCategory.Memorial, TravelCompanion.Friends), 0 },
        // Monument
        { (PlaceCategory.Monument, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Monument, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Monument, TravelCompanion.Family), 10 },
        { (PlaceCategory.Monument, TravelCompanion.Friends), 0 },
        // Museum
        { (PlaceCategory.Museum, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Museum, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Museum, TravelCompanion.Family), 10 },
        { (PlaceCategory.Museum, TravelCompanion.Friends), 0 },
        // Park
        { (PlaceCategory.Park, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Park, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Park, TravelCompanion.Family), 20 },
        { (PlaceCategory.Park, TravelCompanion.Friends), 10 },
        // Restaurant
        { (PlaceCategory.Restaurant, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Restaurant, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Restaurant, TravelCompanion.Family), 20 },
        { (PlaceCategory.Restaurant, TravelCompanion.Friends), 20 },
        // Shopping (place category)
        { (PlaceCategory.Shopping, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Shopping, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Shopping, TravelCompanion.Family), 10 },
        { (PlaceCategory.Shopping, TravelCompanion.Friends), 20 },
        // Supermarket
        { (PlaceCategory.Supermarket, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Supermarket, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Supermarket, TravelCompanion.Family), 10 },
        { (PlaceCategory.Supermarket, TravelCompanion.Friends), 10 },
        // Theater
        { (PlaceCategory.Theater, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Theater, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Theater, TravelCompanion.Family), 10 },
        { (PlaceCategory.Theater, TravelCompanion.Friends), 10 },
        // ThemePark
        { (PlaceCategory.ThemePark, TravelCompanion.Solo), 0 },
        { (PlaceCategory.ThemePark, TravelCompanion.Couple), 10 },
        { (PlaceCategory.ThemePark, TravelCompanion.Family), 20 },
        { (PlaceCategory.ThemePark, TravelCompanion.Friends), 20 },
        // Tower
        { (PlaceCategory.Tower, TravelCompanion.Solo), 10 },
        { (PlaceCategory.Tower, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Tower, TravelCompanion.Family), 10 },
        { (PlaceCategory.Tower, TravelCompanion.Friends), 10 },
        // Viewpoint
        { (PlaceCategory.Viewpoint, TravelCompanion.Solo), 20 },
        { (PlaceCategory.Viewpoint, TravelCompanion.Couple), 20 },
        { (PlaceCategory.Viewpoint, TravelCompanion.Family), 10 },
        { (PlaceCategory.Viewpoint, TravelCompanion.Friends), 20 },
        // Zoo
        { (PlaceCategory.Zoo, TravelCompanion.Solo), 0 },
        { (PlaceCategory.Zoo, TravelCompanion.Couple), 10 },
        { (PlaceCategory.Zoo, TravelCompanion.Family), 20 },
        { (PlaceCategory.Zoo, TravelCompanion.Friends), 10 },
    };

    // ------------------------------------------------------------------
    // Style Score Table — 27 categories × 11 styles = 297 values
    // ------------------------------------------------------------------
    private static readonly Dictionary<(PlaceCategory Category, TravelStyle Style), int> StyleScoreTable = new()
    {
        // Aquarium
        { (PlaceCategory.Aquarium, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Nature), 10 },
        { (PlaceCategory.Aquarium, TravelStyle.Local), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Relax), 10 },
        { (PlaceCategory.Aquarium, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Aquarium, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Aquarium, TravelStyle.Budget), 10 },
        // Attraction
        { (PlaceCategory.Attraction, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Nature), 0 },
        { (PlaceCategory.Attraction, TravelStyle.Local), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Relax), 0 },
        { (PlaceCategory.Attraction, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Attraction, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Attraction, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Nightlife), 10 },
        { (PlaceCategory.Attraction, TravelStyle.Budget), 10 },
        // Bar
        { (PlaceCategory.Bar, TravelStyle.Romantic), -20 },
        { (PlaceCategory.Bar, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Bar, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Bar, TravelStyle.Nature), -20 },
        { (PlaceCategory.Bar, TravelStyle.Local), 10 },
        { (PlaceCategory.Bar, TravelStyle.Relax), 0 },
        { (PlaceCategory.Bar, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Bar, TravelStyle.Gastronomy), 10 },
        { (PlaceCategory.Bar, TravelStyle.Influencer), 0 },
        { (PlaceCategory.Bar, TravelStyle.Nightlife), 20 },
        { (PlaceCategory.Bar, TravelStyle.Budget), -10 },
        // Beach
        { (PlaceCategory.Beach, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Beach, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Beach, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Beach, TravelStyle.Nature), 20 },
        { (PlaceCategory.Beach, TravelStyle.Local), 0 },
        { (PlaceCategory.Beach, TravelStyle.Relax), 20 },
        { (PlaceCategory.Beach, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Beach, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Beach, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Beach, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Beach, TravelStyle.Budget), 20 },
        // Bridge
        { (PlaceCategory.Bridge, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Bridge, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Bridge, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Bridge, TravelStyle.Nature), 10 },
        { (PlaceCategory.Bridge, TravelStyle.Local), 0 },
        { (PlaceCategory.Bridge, TravelStyle.Relax), 10 },
        { (PlaceCategory.Bridge, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Bridge, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Bridge, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Bridge, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Bridge, TravelStyle.Budget), 0 },
        // Cafe
        { (PlaceCategory.Cafe, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Cafe, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Cafe, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Cafe, TravelStyle.Nature), 0 },
        { (PlaceCategory.Cafe, TravelStyle.Local), 20 },
        { (PlaceCategory.Cafe, TravelStyle.Relax), 20 },
        { (PlaceCategory.Cafe, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Cafe, TravelStyle.Gastronomy), 20 },
        { (PlaceCategory.Cafe, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Cafe, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Cafe, TravelStyle.Budget), 20 },
        // Castle
        { (PlaceCategory.Castle, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Castle, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Castle, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Castle, TravelStyle.Nature), 0 },
        { (PlaceCategory.Castle, TravelStyle.Local), 0 },
        { (PlaceCategory.Castle, TravelStyle.Relax), 0 },
        { (PlaceCategory.Castle, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Castle, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Castle, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Castle, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Castle, TravelStyle.Budget), 0 },
        // Cave
        { (PlaceCategory.Cave, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Cave, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Cave, TravelStyle.Adventure), 20 },
        { (PlaceCategory.Cave, TravelStyle.Nature), 20 },
        { (PlaceCategory.Cave, TravelStyle.Local), 0 },
        { (PlaceCategory.Cave, TravelStyle.Relax), 0 },
        { (PlaceCategory.Cave, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Cave, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Cave, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Cave, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Cave, TravelStyle.Budget), 10 },
        // Church
        { (PlaceCategory.Church, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Church, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Church, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Church, TravelStyle.Nature), 0 },
        { (PlaceCategory.Church, TravelStyle.Local), 10 },
        { (PlaceCategory.Church, TravelStyle.Relax), 0 },
        { (PlaceCategory.Church, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Church, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Church, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Church, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Church, TravelStyle.Budget), 0 },
        // Forest
        { (PlaceCategory.Forest, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Forest, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Forest, TravelStyle.Adventure), 20 },
        { (PlaceCategory.Forest, TravelStyle.Nature), 20 },
        { (PlaceCategory.Forest, TravelStyle.Local), 0 },
        { (PlaceCategory.Forest, TravelStyle.Relax), 10 },
        { (PlaceCategory.Forest, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Forest, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Forest, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Forest, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Forest, TravelStyle.Budget), 10 },
        // Gallery
        { (PlaceCategory.Gallery, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Gallery, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Gallery, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Nature), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Local), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Relax), 10 },
        { (PlaceCategory.Gallery, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Gallery, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Gallery, TravelStyle.Budget), 0 },
        // Historical
        { (PlaceCategory.Historical, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Historical, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Historical, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Historical, TravelStyle.Nature), 0 },
        { (PlaceCategory.Historical, TravelStyle.Local), 10 },
        { (PlaceCategory.Historical, TravelStyle.Relax), 0 },
        { (PlaceCategory.Historical, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Historical, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Historical, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Historical, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Historical, TravelStyle.Budget), 10 },
        // Information
        { (PlaceCategory.Information, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Information, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Information, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Information, TravelStyle.Nature), 0 },
        { (PlaceCategory.Information, TravelStyle.Local), 10 },
        { (PlaceCategory.Information, TravelStyle.Relax), 0 },
        { (PlaceCategory.Information, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Information, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Information, TravelStyle.Influencer), 0 },
        { (PlaceCategory.Information, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Information, TravelStyle.Budget), 10 },
        // Mall
        { (PlaceCategory.Mall, TravelStyle.Romantic), -10 },
        { (PlaceCategory.Mall, TravelStyle.Cultural), -20 },
        { (PlaceCategory.Mall, TravelStyle.Adventure), -20 },
        { (PlaceCategory.Mall, TravelStyle.Nature), -20 },
        { (PlaceCategory.Mall, TravelStyle.Local), 0 },
        { (PlaceCategory.Mall, TravelStyle.Relax), 0 },
        { (PlaceCategory.Mall, TravelStyle.Shopping), 20 },
        { (PlaceCategory.Mall, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Mall, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Mall, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Mall, TravelStyle.Budget), -10 },
        // Market
        { (PlaceCategory.Market, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Market, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Market, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Market, TravelStyle.Nature), 0 },
        { (PlaceCategory.Market, TravelStyle.Local), 20 },
        { (PlaceCategory.Market, TravelStyle.Relax), 0 },
        { (PlaceCategory.Market, TravelStyle.Shopping), 20 },
        { (PlaceCategory.Market, TravelStyle.Gastronomy), 20 },
        { (PlaceCategory.Market, TravelStyle.Influencer), 0 },
        { (PlaceCategory.Market, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Market, TravelStyle.Budget), 20 },
        // Memorial
        { (PlaceCategory.Memorial, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Memorial, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Memorial, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Memorial, TravelStyle.Nature), 0 },
        { (PlaceCategory.Memorial, TravelStyle.Local), 10 },
        { (PlaceCategory.Memorial, TravelStyle.Relax), 0 },
        { (PlaceCategory.Memorial, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Memorial, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Memorial, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Memorial, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Memorial, TravelStyle.Budget), 20 },
        // Monument
        { (PlaceCategory.Monument, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Monument, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Monument, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Monument, TravelStyle.Nature), 0 },
        { (PlaceCategory.Monument, TravelStyle.Local), 10 },
        { (PlaceCategory.Monument, TravelStyle.Relax), 0 },
        { (PlaceCategory.Monument, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Monument, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Monument, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Monument, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Monument, TravelStyle.Budget), 20 },
        // Museum
        { (PlaceCategory.Museum, TravelStyle.Romantic), 10 },
        { (PlaceCategory.Museum, TravelStyle.Cultural), 20 },
        { (PlaceCategory.Museum, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Museum, TravelStyle.Nature), 0 },
        { (PlaceCategory.Museum, TravelStyle.Local), 10 },
        { (PlaceCategory.Museum, TravelStyle.Relax), 10 },
        { (PlaceCategory.Museum, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Museum, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Museum, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Museum, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Museum, TravelStyle.Budget), 0 },
        // Park
        { (PlaceCategory.Park, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Park, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Park, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Park, TravelStyle.Nature), 20 },
        { (PlaceCategory.Park, TravelStyle.Local), 10 },
        { (PlaceCategory.Park, TravelStyle.Relax), 20 },
        { (PlaceCategory.Park, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Park, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Park, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Park, TravelStyle.Nightlife), -10 },
        { (PlaceCategory.Park, TravelStyle.Budget), 20 },
        // Restaurant
        { (PlaceCategory.Restaurant, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Restaurant, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Restaurant, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Restaurant, TravelStyle.Nature), 0 },
        { (PlaceCategory.Restaurant, TravelStyle.Local), 20 },
        { (PlaceCategory.Restaurant, TravelStyle.Relax), 10 },
        { (PlaceCategory.Restaurant, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Restaurant, TravelStyle.Gastronomy), 20 },
        { (PlaceCategory.Restaurant, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Restaurant, TravelStyle.Nightlife), 10 },
        { (PlaceCategory.Restaurant, TravelStyle.Budget), 0 },
        // Shopping (place category)
        { (PlaceCategory.Shopping, TravelStyle.Romantic), 0 },
        { (PlaceCategory.Shopping, TravelStyle.Cultural), -20 },
        { (PlaceCategory.Shopping, TravelStyle.Adventure), -20 },
        { (PlaceCategory.Shopping, TravelStyle.Nature), -20 },
        { (PlaceCategory.Shopping, TravelStyle.Local), 10 },
        { (PlaceCategory.Shopping, TravelStyle.Relax), 0 },
        { (PlaceCategory.Shopping, TravelStyle.Shopping), 20 },
        { (PlaceCategory.Shopping, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Shopping, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Shopping, TravelStyle.Nightlife), 10 },
        { (PlaceCategory.Shopping, TravelStyle.Budget), 0 },
        // Supermarket
        { (PlaceCategory.Supermarket, TravelStyle.Romantic), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Cultural), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Adventure), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Nature), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Local), 20 },
        { (PlaceCategory.Supermarket, TravelStyle.Relax), 0 },
        { (PlaceCategory.Supermarket, TravelStyle.Shopping), 10 },
        { (PlaceCategory.Supermarket, TravelStyle.Gastronomy), 10 },
        { (PlaceCategory.Supermarket, TravelStyle.Influencer), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Supermarket, TravelStyle.Budget), 20 },
        // Theater
        { (PlaceCategory.Theater, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Theater, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Theater, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Theater, TravelStyle.Nature), 0 },
        { (PlaceCategory.Theater, TravelStyle.Local), 10 },
        { (PlaceCategory.Theater, TravelStyle.Relax), 10 },
        { (PlaceCategory.Theater, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Theater, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Theater, TravelStyle.Influencer), 10 },
        { (PlaceCategory.Theater, TravelStyle.Nightlife), 20 },
        { (PlaceCategory.Theater, TravelStyle.Budget), 0 },
        // ThemePark
        { (PlaceCategory.ThemePark, TravelStyle.Romantic), -20 },
        { (PlaceCategory.ThemePark, TravelStyle.Cultural), -20 },
        { (PlaceCategory.ThemePark, TravelStyle.Adventure), 10 },
        { (PlaceCategory.ThemePark, TravelStyle.Nature), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Local), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Relax), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Shopping), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Influencer), 20 },
        { (PlaceCategory.ThemePark, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.ThemePark, TravelStyle.Budget), -10 },
        // Tower
        { (PlaceCategory.Tower, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Tower, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Tower, TravelStyle.Adventure), 10 },
        { (PlaceCategory.Tower, TravelStyle.Nature), 10 },
        { (PlaceCategory.Tower, TravelStyle.Local), 0 },
        { (PlaceCategory.Tower, TravelStyle.Relax), 0 },
        { (PlaceCategory.Tower, TravelStyle.Shopping), 0 },
        { (PlaceCategory.Tower, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Tower, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Tower, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Tower, TravelStyle.Budget), 0 },
        // Viewpoint
        { (PlaceCategory.Viewpoint, TravelStyle.Romantic), 20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Cultural), 10 },
        { (PlaceCategory.Viewpoint, TravelStyle.Adventure), 20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Nature), 20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Local), 0 },
        { (PlaceCategory.Viewpoint, TravelStyle.Relax), 20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Viewpoint, TravelStyle.Influencer), 20 },
        { (PlaceCategory.Viewpoint, TravelStyle.Nightlife), 0 },
        { (PlaceCategory.Viewpoint, TravelStyle.Budget), 10 },
        // Zoo
        { (PlaceCategory.Zoo, TravelStyle.Romantic), -20 },
        { (PlaceCategory.Zoo, TravelStyle.Cultural), 0 },
        { (PlaceCategory.Zoo, TravelStyle.Adventure), 0 },
        { (PlaceCategory.Zoo, TravelStyle.Nature), 10 },
        { (PlaceCategory.Zoo, TravelStyle.Local), 0 },
        { (PlaceCategory.Zoo, TravelStyle.Relax), 10 },
        { (PlaceCategory.Zoo, TravelStyle.Shopping), -20 },
        { (PlaceCategory.Zoo, TravelStyle.Gastronomy), 0 },
        { (PlaceCategory.Zoo, TravelStyle.Influencer), 0 },
        { (PlaceCategory.Zoo, TravelStyle.Nightlife), -20 },
        { (PlaceCategory.Zoo, TravelStyle.Budget), 10 },
    };

    // ------------------------------------------------------------------
    // Google Tag Mapping — PRD 4.4
    // ------------------------------------------------------------------
    private static readonly Dictionary<TravelStyle, List<string>> GoogleTagMapping = new()
    {
        { TravelStyle.Romantic,    new() { "Relaxation", "Beach", "City" } },
        { TravelStyle.Cultural,    new() { "Cultural", "Historical", "Educational", "Art" } },
        { TravelStyle.Adventure,   new() { "Adventure", "Hiking", "Nature" } },
        { TravelStyle.Nature,      new() { "Nature", "Beach", "Hiking" } },
        { TravelStyle.Local,       new() { "Food & Drink", "City", "Shopping" } },
        { TravelStyle.Relax,       new() { "Relaxation", "Beach", "Nature" } },
        { TravelStyle.Shopping,    new() { "Shopping" } },
        { TravelStyle.Gastronomy,  new() { "Food & Drink" } },
        { TravelStyle.Influencer,  new() { "Photography", "Beach", "City" } },
        { TravelStyle.Nightlife,   new() { "Nightlife", "Entertainment" } },
        { TravelStyle.Budget,      new() { "Food & Drink", "City", "Shopping" } },
    };

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public int CalculateGroupScore(PlaceCategory category, TravelCompanion companion)
    {
        return GroupScoreTable.TryGetValue((category, companion), out var score)
            ? score
            : 0;
    }

    public int CalculateStyleScore(PlaceCategory category, TravelStyle style)
    {
        return StyleScoreTable.TryGetValue((category, style), out var score)
            ? score
            : 0;
    }

    public int CalculateStyleScoreAverage(PlaceCategory category, List<TravelStyle> styles)
    {
        if (styles == null || styles.Count == 0)
            return 0;

        var sum = 0;
        foreach (var style in styles)
        {
            sum += CalculateStyleScore(category, style);
        }

        return sum / styles.Count;
    }

    public int CalculateGoogleMatchBonus(List<string> googleTags, List<TravelStyle> selectedStyles)
    {
        if (googleTags == null || googleTags.Count == 0 || selectedStyles == null || selectedStyles.Count == 0)
            return 0;

        var bonus = 0;
        foreach (var style in selectedStyles)
        {
            if (!GoogleTagMapping.TryGetValue(style, out var mappedTags))
                continue;

            foreach (var tag in googleTags)
            {
                if (mappedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    bonus += 10;
                    break; // one match per style is enough
                }
            }
        }

        return bonus;
    }

    public int CalculateFinalScore(
        PlaceCategory category,
        TravelCompanion companion,
        List<TravelStyle> styles,
        List<string> googleTags)
    {
        var groupScore = CalculateGroupScore(category, companion);
        var styleAvg = CalculateStyleScoreAverage(category, styles);
        var bonus = CalculateGoogleMatchBonus(googleTags, styles);

        return groupScore + styleAvg + bonus;
    }

    public List<ScoredPlaceResult> ScoreAndSortPlaces(
        List<Place> places,
        TravelCompanion companion,
        List<TravelStyle> styles)
    {
        var results = new List<ScoredPlaceResult>(places.Count);

        foreach (var place in places)
        {
            var groupScore = CalculateGroupScore(place.Category, companion);
            var styleAvg = CalculateStyleScoreAverage(place.Category, styles);
            var bonus = CalculateGoogleMatchBonus(place.GoogleTags, styles);
            var final = groupScore + styleAvg + bonus;

            results.Add(new ScoredPlaceResult
            {
                Place = place,
                GroupScore = groupScore,
                StyleScoreAvg = styleAvg,
                GoogleMatchBonus = bonus,
                FinalScore = final,
            });
        }

        return results.OrderByDescending(r => r.FinalScore).ToList();
    }
}
