# CLI File Reader Library
A .NET library developed for the CodeBricks research project that is
specifically designed to read and rewrite .NET binaries and was used
in conjuction with the Visual Storms / Sharp Storms projects.

Original Project Website – https://archive.codeplex.com/?p=clifilerw


Copyright © 2002,2003 Antonio Cisternino (cisterni@di.unipi.it)

This library has been implemented  to provide access to the raw format
of CLI files. The library is  still under development, so you can find
some problem in its use.

The  library  has been  developed  using  CLI standard  documentation.
Because fast access  to files is a goal I use  memory mapping in order
to have fast access to the  file. This implies that you need a certain
amount of privileges to use it.

The  library just  exposes  the content  of  the CLI  file. There  are
facilities   to    help   the   programmer    to   interoperate   with
System.Reflection  but these  facilities  are distinct  from the  base
functionality. For instance whenever a string is required its index is
returned.  The programmer  must explictly  use the  Strings  object in
order to get a string.
