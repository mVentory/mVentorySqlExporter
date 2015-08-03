-- This script extracts all product data from CI database into a single flat table.
declare @ibranch int -- default branch ID for internet pricing
set @ibranch = 1

-- Declare a temp table for all the stock data with a flat structure
declare @scs as table (
	id int primary key, 
	styleid int, 
	colorid int, 
	sizeid int,
	lookupnum int, 
	size nvarchar(15), 
	sizetm nvarchar(50),
	color nvarchar(15),
	price decimal(9,2),
	special decimal(9,2),
	quantity int,
	code nvarchar(15),
	descr nvarchar(40),
	idescrshort nvarchar(255),
	idescrfull nvarchar(max),
	category nvarchar(255),
	branches nvarchar(max),
	stockgroup nvarchar(255),
	stockgroupid int,
	brand nvarchar(255),
	imgids nvarchar(255)
)

-- Populare the temp table with data from several different table. It's faster than doing multiple joins.
insert into @scs (id, styleid, colorid, sizeid, lookupnum) SELECT [ID],[Style ID],[Colour ID],[Size ID],[Look Up Number] FROM [Style Colour Size]
update @scs set code=ss.Code, descr=ss.Description, idescrshort=ss.[Internet Short Description], idescrfull=ss.[Internet Description], stockgroupid=ss.[Stock Group ID]  from @scs scs, [Stock Style] ss where scs.styleid=ss.id
update @scs set stockgroup=sg.Code from @scs scs, [Stock Group] sg where scs.stockgroupid =sg.id
update @scs set size=ss.Code, sizetm=substring(isnull(stockgroup,''),1,2)+'_'+isnull(ss.Code,'') from @scs scs, [Stock Size] ss where scs.sizeid=ss.id
update @scs set color=sc.Code from @scs scs, [Stock Colour] sc where scs.colorid=sc.id

-- Populate price data
declare @price as table ([Style/Colour/Size ID] int PRIMARY KEY, [Selling Price 1] decimal(9,2), [Selling Price 2] decimal(9,2), styleid int)
insert into @price 
	select [Style/Colour/Size ID], round(isnull([Selling Price 1],0)*1.15, 2,1), round(isnull([Selling Price 2],0)*1.15, 2,1), null from [Stock Detail] where [Branch ID]=@ibranch and [Selling Price 1]>0
update @price set styleid=scs.styleid from @price p, @scs scs where p.[Style/Colour/Size ID]=scs.id -- get prices per style/size/color
update @scs set price=p.[Selling Price 1], special=p.[Selling Price 2] from @scs scs, @price p where scs.id=p.[Style/Colour/Size ID] -- update per style/size/color, which doesn't cover all products

-- Copy price info per styleid, but rearrange the keys for faster updates first
declare @price2 as table ([Selling Price 1] decimal(9,2), styleid int PRIMARY KEY)
insert into @price2 (styleid, [Selling Price 1]) select styleid, max(isnull([Selling Price 1],0)) from @price where styleid is not null group by styleid
update @scs set price=p.[Selling Price 1] from @scs scs, @price2 p where scs.styleid=p.styleid and (scs.price is null or scs.price<0) -- copy prices between sizes
-- repeat the same for special price
delete from @price2
insert into @price2 (styleid, [Selling Price 1]) select styleid, max(isnull([Selling Price 2],0)) from @price where styleid is not null group by styleid
update @scs set special=p.[Selling Price 1] from @scs scs, @price2 p where scs.styleid=p.styleid and (scs.special is null or scs.special<0) -- copy prices between sizes
delete from @price
delete from @price2

-- remove records that make no sense (we don't know if this is correct, though, temp plug)
delete from @scs where price is null or price = 0
delete from @scs where lookupnum=0 -- delete the configurable product information - those records have a non-existent lookupnum=0

-- do the same with quantities
declare @qty as table ([Style/Colour/Size ID] int PRIMARY KEY, quantity int)
insert into @qty select [Style/Colour/Size ID], sum(isnull(quantity,0)) as quantity from [Stock On Hand] soh group by [Style/Colour/Size ID]
update @scs set quantity=q.quantity from @scs scs, @qty q where scs.id=q.[Style/Colour/Size ID]
delete from @qty

-- delete from @scs where quantity is null or quantity < 1 

-- update quantities per branch
-- prebuild quantity sums - there may be multiple records per item per branch. They need to be added up.
declare @soh table (id int, bid int, qty int)
insert into @soh select [Style/Colour/Size ID], [Branch ID], sum(isnull(Quantity,0)) from [Stock On Hand] where isnull(Quantity,0)!=0 group by [Style/Colour/Size ID], [Branch ID]
-- loop though every branch and contatenate names of branches where the product is stocked
declare @branchid int, @branchname nvarchar(500)
declare branch_cursor CURSOR FOR select id, Description from Branch
open branch_cursor
FETCH NEXT FROM branch_cursor into @branchid, @branchname
WHILE @@FETCH_STATUS = 0
 BEGIN
	update @scs set branches=isnull(branches,'') + isnull(@branchname,'') + ':' + convert(nvarchar(20),isnull(soh.qty,0)) + ';'  from @scs scs, @soh soh where soh.id=scs.id and soh.bid=@branchid and soh.qty>0
	FETCH NEXT FROM branch_cursor into @branchid, @branchname
 END
CLOSE branch_cursor
DEALLOCATE branch_cursor

-- prepare the list of categories (up to 3 levels)
declare @cats table (title nvarchar(255), id int PRIMARY KEY, pid int)
insert into @cats SELECT t.title, t.ID, t.[parent id] FROM [Internet Group] t 
update @cats set title = isnull(t.title,'') + '/' + isnull(c.title,''), pid=t.[parent id] from @cats c, [Internet Group] t where c.pid = t.ID
update @cats set title = isnull(t.title,'') + '/' + isnull(c.title,''), pid=t.[parent id] from @cats c, [Internet Group] t where c.pid = t.ID

-- build list of categories per product by looping through all categories one at a time and update products in that category
-- there is a simpler solution using XPATH(''), but it's not documented and may be deprecated any time.
declare @catid int, @cattitle nvarchar(255)
declare cat_cursor CURSOR FOR select id, title from @cats
open cat_cursor
FETCH NEXT FROM cat_cursor into @catid, @cattitle

WHILE @@FETCH_STATUS = 0
BEGIN
	update @scs set category =isnull(category,'') + ';' + isnull(@cattitle,'') from @scs s, [Stock Internet Group] ig where ig.[Internet Group ID]=@catid and s.styleid=ig.[Stock Style ID]
	FETCH NEXT FROM cat_cursor into @catid, @cattitle
END
CLOSE cat_cursor
DEALLOCATE cat_cursor

--add brand as a separate field even if it comes from categories
update @scs set brand=title 
	from @scs s, [Stock Internet Group] sig, [Internet Group] ig 
	where ig.[ID]=sig.[Internet Group ID] and s.styleid=sig.[Stock Style ID] and ig.[parent id]=8 -- 8 is ID of Brands in the category list

--build list of images
declare @imgs table(imgids nvarchar(255), scsid int PRIMARY KEY, ssid int, cid int) -- a temp table for a ;-separated list of images per product
-- prepopulate the table with the list of products
insert into @imgs (scsid,ssid,cid) select distinct scs.id, im.[Stock Style ID], im.[Stock Colour ID] from [Stock Image Map] im, @scs scs where im.[Stock Style ID]=scs.styleid and im.[Stock Colour ID]=scs.colorid
-- loop through every image and build a list in the temp table
declare @imgid int, @cid int, @ssid int
declare img_cursor CURSOR FOR select ID, [Stock Style ID], [Stock Colour ID] from [Stock Image Map]
open img_cursor
FETCH NEXT FROM img_cursor into @imgid, @ssid, @cid
WHILE @@FETCH_STATUS = 0
BEGIN
	update @imgs set imgids =isnull(imgids,'') + ';' + CONVERT(nvarchar(255), @imgid) + '.jpg' where ssid=@ssid and cid=@cid
	FETCH NEXT FROM img_cursor into @imgid, @ssid, @cid
END
CLOSE img_cursor
DEALLOCATE img_cursor
-- copy image IDs into the main table 
update @scs set imgids=imgs.imgids from @scs scs, @imgs imgs where imgs.scsid=scs.id


-- return a single flatened data structure
select * from @scs order by styleid desc, colorid, sizeid
