// ***********************************************************************************
// File: CspProject/Models/FmeaRating.cs
// Description: Data model for holding rating scale information.
// Author: Enes Orak
// ***********************************************************************************

namespace CspProject.Models;

public record FmeaRating(int Score, string Title, string Description, string FullText)
{
    public FmeaRating(int score, string title, string description)
        : this(score, title, description, $"{score} - {title}") { }
}