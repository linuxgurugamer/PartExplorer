ScienceMods configuration files
===============================

Each .cfg file can contain one or more module definitions. A definition may use
the simple bare-brace form below:

{
    ModuleName = MyScienceModule

    expField_1 = experimentID
    expField_1_Title = Experiment ID

    expField_2 = experimentValue
    expField_2_TitleField = experimentTitle
}

ModuleName
    The PartModule name to match.

expField_#
    The field to read from the part's MODULE config. Numbers control display order.

expField_#_Title
    Optional fixed label shown for that field.

expField_#_TitleField
    Optional name of another field in the same MODULE whose value supplies the
    display label. If that field is missing/blank, expField_#_Title is used.

Resource handling
-----------------
Resource nodes do not normally need to be listed in the ScienceMods file.
For a matched science module, Part Explorer automatically detects:

    INPUT_RESOURCE / INPUTRESOURCE
    OUTPUT_RESOURCE / OUTPUTRESOURCE
    RESOURCE

An unlabeled RESOURCE node is treated as an input resource; this is the form
used by DMagic Orbital Science for ElectricCharge consumption.

The following common field-based resource declarations are also recognized:

    resourceToUse + resourceCost          -> input
    experimentResource + resourceCost     -> input
    reactant + reactantPerProduct          -> input
    product + productPerHour               -> output

Bundled definitions
-------------------
The package includes explicit science-module definitions for:

    SCANsat
    Wild Blue Industries WBIScience 1.5.1
    Nehemiah Engineering Orbital Science 0.10.0
    DMagic Orbital Science 1.4.3
    Station Science Continued 2.6.0
    LTech Continued 1-0.5.3.2
    James Webb for Kerbal 1.12.x
    Impact 1.9.2
    ExoInstruments 0.5.1
    Tarsier Space Technology Continued 7.13
    Microbiology Expansion 0.2.0

Reusable stock KSP definitions are also included for:

    ModuleScienceExperiment
    ModuleScienceLab
    ModuleScienceConverter

The following scanned mods are covered by those stock definitions and do not
need separate custom definition files:

    KrakenScience 1.0
    Mkerb Inc. Science Instruments 1.1
    KDEX 2.0.2
    Interkosmos 0.5
    Planetside Exploration Technologies 1.0.2

Some supported mods use both custom modules and stock science modules. Their
custom modules are defined in their named .cfg file and their stock modules are
handled by the reusable stock definitions above.

Definitions are reloaded when the part database is refreshed.
