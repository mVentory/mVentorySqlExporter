-- This script extracts all product data from CI database into a single flat table.

-- Declare a temp table for all the stock data with a flat structure
declare @scs as table (
	id int primary key, 
	styleid int, 
	colorid int, 
	sizeid int, 
	lookupnum int, 
	size nvarchar(15), 
	color nvarchar(15),
	price float,
	quantity int,
	code nvarchar(15),
	descr nvarchar(40),
	idescrshort nvarchar(255),
	idescrfull nvarchar(max),
	category nvarchar(255)
	)

-- Populare the temp table with data from several different table. It's faster than doing multiple joins.
insert into @scs (id, styleid, colorid, sizeid, lookupnum) SELECT [ID],[Style ID],[Colour ID],[Size ID],[Look Up Number] FROM [Style Colour Size]
update @scs set size=ss.Code from @scs scs, [Stock Size] ss where scs.sizeid=ss.id
update @scs set color=sc.Code from @scs scs, [Stock Colour] sc where scs.colorid=sc.id
update @scs set code=ss.Code, descr=ss.Description, idescrshort=ss.[Internet Short Description], idescrfull=ss.[Internet Description]  from @scs scs, [Stock Style] ss where scs.styleid=ss.id

-- Populate price data
declare @price as table ([Style/Colour/Size ID] int PRIMARY KEY, [Selling Price 1] decimal, styleid int)
insert into @price 
select [Style/Colour/Size ID], max([Selling Price 1]), null from [Stock Detail] where [Selling Price 1]>0 group by [Style/Colour/Size ID]
update @price set styleid=scs.styleid from @price p, @scs scs where p.[Style/Colour/Size ID]=scs.id
update @scs set price=p.[Selling Price 1] from @scs scs, @price p where scs.id=p.[Style/Colour/Size ID] -- usually only one product per style has a price

-- Copy price info per styleid, but rearrange the keys first
declare @price2 as table ([Selling Price 1] decimal, styleid int PRIMARY KEY)
insert into @price2 (styleid, [Selling Price 1]) select styleid, max([Selling Price 1]) from @price where styleid is not null group by styleid
update @scs set price=p.[Selling Price 1] from @scs scs, @price2 p where scs.styleid=p.styleid and scs.price is null -- copy prices between sizes
delete from @price
delete from @price2

-- remove records that make no sense (we don't know if this is correct, though, temp plug)
-- delete from @scs where price is null or price = 0

-- do the same with quantities
declare @qty as table ([Style/Colour/Size ID] int PRIMARY KEY, quantity int)
insert into @qty select [Style/Colour/Size ID], sum(quantity) as quantity from [Stock On Hand] soh 
	where exists (
			select 1 
				from (select [Style/Colour/Size ID], [Branch ID], max([Date]) as [Date] from [Stock On Hand] group by [Style/Colour/Size ID], [Branch ID]) soh2 
				where soh.[Style/Colour/Size ID]=soh2.[Style/Colour/Size ID] and soh.Date=soh2.Date and soh.[Branch ID]=soh2.[Branch ID])
	group by [Style/Colour/Size ID]

update @scs set quantity=q.quantity from @scs scs, @qty q where scs.id=q.[Style/Colour/Size ID]
delete from @qty

delete from @scs where quantity is null or quantity < 1 

-- prepare the list of categories (up to 3 levels)
declare @cats table (title nvarchar(255), id int PRIMARY KEY, pid int)
insert into @cats SELECT t.title, t.ID, t.[parent id] FROM [Internet Group] t 
update @cats set title = CONCAT(t.title,'/',c.title), pid=t.[parent id] from @cats c, [Internet Group] t where c.pid = t.ID
update @cats set title = CONCAT(t.title,'/',c.title), pid=t.[parent id] from @cats c, [Internet Group] t where c.pid = t.ID

-- build list of categories per product by looping through all categories one at a time and update products in that category
-- there is a simpler solution using XPATH(''), but it's not documented and may be deprecated any time.
declare @catid int, @cattitle nvarchar(255)
declare cat_cursor CURSOR FOR select id, title from @cats
open cat_cursor
FETCH NEXT FROM cat_cursor into @catid, @cattitle

WHILE @@FETCH_STATUS = 0
BEGIN
	update @scs set category =concat(category, ';', @cattitle) from @scs s, [Stock Internet Group] ig where ig.[Internet Group ID]=@catid and s.styleid=ig.[Stock Style ID]
	FETCH NEXT FROM cat_cursor into @catid, @cattitle
END
CLOSE cat_cursor
DEALLOCATE cat_cursor

-- return a single flatened data structure
select * from @scs order by styleid, sizeid, colorid
