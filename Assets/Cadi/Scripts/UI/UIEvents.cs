using Cadi.Scripts.EventSystem;
using Cadi.Scripts.UI.GraphicSystems;

namespace Cadi.Scripts.UI
{
    public class UIEvents
    {

    }

    public class SGraphixSelectedEvent : Event<SGraphixSelectedEvent>
    {
        public ISelectiveGraphix SelectiveGraphix { get; private set; }

        public static SGraphixSelectedEvent Rent(ISelectiveGraphix selectiveGraphix)
        {
            var e = Rent();
            e.SelectiveGraphix = selectiveGraphix;
            return e;
        }
    }

    public class SGraphixDeselectedEvent : Event<SGraphixDeselectedEvent>
    {
        public ISelectiveGraphix SelectiveGraphix { get; private set; }

        public static SGraphixDeselectedEvent Rent(ISelectiveGraphix selectiveGraphix)
        {
            var e = Rent();
            e.SelectiveGraphix = selectiveGraphix;
            return e;
        }
    }

    public class SGraphixCreatedEvent : Event<SGraphixCreatedEvent>
    {
        public ISelectiveGraphix SelectiveGraphix { get; private set; }

        public static SGraphixCreatedEvent Rent(ISelectiveGraphix selectiveGraphix)
        {
            var e = Rent();
            e.SelectiveGraphix = selectiveGraphix;
            return e;
        }
    }

}
