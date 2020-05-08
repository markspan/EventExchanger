from .evtexchanger import EvtExchanger

_evtLister = EvtExchanger().Device()
_evtLister.Select("EventExchanger")
