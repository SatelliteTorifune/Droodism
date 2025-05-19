using ModApi.Craft;
using ModApi.Craft.Parts;
using System;
using System.Collections.Generic;


namespace ModApi.Design.Events
{
    public class NewPartAddedEvent : EventArgs
    {
        public NewPartAddedEvent(
            PartData partData,
            IReadOnlyList<IPartScript> partScripts)
        {
            this.PartData = partData;
        }

        public PartData PartData { get; }
        
        public IReadOnlyList<IPartScript> PartScripts { get; }
    }

}
    
